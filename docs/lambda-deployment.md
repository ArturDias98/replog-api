# Lambda Deployment Guide

How the replog backend is packaged and deployed as two AWS Lambda functions behind one API Gateway HTTP API (v2). This document is the contract for the infrastructure repo that owns the CloudFormation stack.

## Architecture

```text
client → API Gateway (HTTP API v2)
           ├── /api/auth/*   → Lambda (replog-api-auth) → DynamoDB
           │                                            → Google OAuth (login)
           └── /api/sync/*   → Lambda (replog-api)      → DynamoDB
```

Each Lambda boots its own ASP.NET Core pipeline (middleware, JWT auth, CORS, endpoints) via `Amazon.Lambda.AspNetCoreServer.Hosting`. Shared bootstrap (JWT bearer config, CORS, exception handler, secrets loader, health endpoint) lives in the `replog-api-host` class library, so each `Program.cs` stays small.

The auth Lambda issues JWTs at `/api/auth/login` and `/api/auth/refresh`. The sync Lambda receives those JWTs via the `access_token` cookie and verifies them locally — both Lambdas share the same HS256 signing secret (see "Required Environment Variables").

## Local vs Lambda

`Program.cs` calls `builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi)` in both hosts. Auto-detecting:

- **Outside Lambda** (`AWS_LAMBDA_FUNCTION_NAME` env var absent): no-op. `dotnet run` launches Kestrel.
- **Inside Lambda** (env var present): API Gateway HTTP API v2 events drive the request pipeline.

No code branching — the same artifact runs everywhere.

## Building the Deployment Artifacts

```bash
dotnet tool install -g Amazon.Lambda.Tools

# Sync Lambda
dotnet lambda package -pl replog-api -c Release -o bin/Release/replog-api.zip

# Auth Lambda
dotnet lambda package -pl replog-api-auth -c Release -o bin/Release/replog-api-auth.zip
```

CI does both via a matrix over `[replog-api, replog-api-auth]` in `.github/workflows/deploy.yml`.

## Function Configuration

Both functions share the same defaults (override per function as needs diverge).

| Setting | Value |
| --- | --- |
| Runtime | `dotnet8` (managed) |
| Architecture | `x86_64` (or `arm64` for ~20% cheaper / faster) |
| Memory | 1024 MB |
| Timeout | 30 s |
| Handler | assembly name (`replog-api` or `replog-api-auth`) — required by `AddAWSLambdaHosting` |

Defaults live in each project's `aws-lambda-tools-defaults.json`.

## Required Environment Variables

| Variable | replog-api-auth | replog-api (sync) | Notes |
| --- | --- | --- | --- |
| `JWT_SECRET_ARN` | required | required | ARN of the Secrets Manager secret holding the JWT HS256 signing key. Both Lambdas need it: auth signs, sync verifies. Resolved at cold start by [`SecretsLoader`](../replog-api-host/SecretsLoader.cs) and bound to `Jwt:Secret`. |
| `GOOGLE_CLIENT_ID_ARN` | required | — | ARN of the Secrets Manager secret holding the Google OAuth client ID. Auth-only. Bound to `Google:ClientId`. |
| `Jwt__Issuer` | `replog-api` | `replog-api` | default |
| `Jwt__Audience` | `replog-client` | `replog-client` | default |
| `Jwt__AccessTokenExpirationMinutes` | `15` | — | sync host doesn't issue tokens |
| `Jwt__RefreshTokenExpirationDays` | `30` | — | sync host doesn't issue tokens |
| `Jwt__AccessTokenCookieExpirationDays` | `30` | — | sync host doesn't issue tokens |
| `Jwt__RefreshTokenCookieExpirationDays` | `30` | — | sync host doesn't issue tokens |
| `DynamoDB__Region` | required | required | e.g. `us-east-1` |
| `DynamoDB__TableName` | required | required | `replog-workouts` |
| `DynamoDB__UsersTableName` | required | required | `replog-users` |
| `RateLimiter__PermitLimit` | — | optional | per-user requests/minute on `/api/sync/*` |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Production` | gates the Secrets Manager fetch |

Double-underscore (`__`) maps to ASP.NET configuration section delimiter (`Jwt:Secret`, etc.).

Secrets are stored in AWS Secrets Manager as plain text (`SecretString`). Each Lambda fetches them at cold start using its execution-role credentials — so the `SecretsManagerReadPolicy` IAM block in the CloudFormation stack must grant `secretsmanager:GetSecretValue` on the ARNs each function needs (auth needs both; sync only needs `JWT_SECRET_ARN`). Resolution is gated on `ASPNETCORE_ENVIRONMENT=Production`; in any other environment `SecretsLoader` is a no-op and `Jwt:Secret` / `Google:ClientId` come from `appsettings.{Environment}.json` as before. Secret rotation propagates on the next cold start with no redeploy needed.

## API Gateway (HTTP API v2) Routing

Two integrations — one per Lambda. Routes:

| Route | Integration |
| --- | --- |
| `ANY /api/auth/{proxy+}` | replog-api-auth |
| `GET /api/auth/health` | replog-api-auth |
| `ANY /api/sync/{proxy+}` | replog-api |
| `GET /api/sync/health` | replog-api |

Each integration also needs a `lambda:InvokeFunction` permission scoped to the API Gateway source ARN.

CORS (must allow credentials so cookies flow):

```yaml
AllowOrigins:   [https://replog.adrvcode.com, http://localhost:4200]
AllowMethods:   [GET, POST]
AllowHeaders:   [Content-Type]
AllowCredentials: true
MaxAge:         600
```

Throttling: stage-level default 100 RPS / burst 200 is a reasonable starting point. Per-user rate limiting is enforced inside the sync Lambda (`RequireRateLimiting("sync")`), not at API Gateway.

## IAM Permissions for the Lambda Execution Roles

The two functions can share one role (same DynamoDB tables and Secrets Manager ARNs), or be split into two least-privilege roles — the sync function does not need `secretsmanager:GetSecretValue` on the Google client ID ARN.

Common permissions for both:
- `dynamodb:GetItem`, `PutItem`, `UpdateItem`, `DeleteItem`, `Query`, `BatchWriteItem`, `TransactWriteItems`, `DescribeTable` on the `replog-workouts` table + its `UserIdIndex` GSI
- Same R/W on `replog-users`
- `logs:CreateLogGroup`, `CreateLogStream`, `PutLogEvents` (or use the AWSLambdaBasicExecutionRole managed policy)
- `secretsmanager:GetSecretValue` on `JWT_SECRET_ARN`

Auth-only:
- `secretsmanager:GetSecretValue` on `GOOGLE_CLIENT_ID_ARN`

## Logging & Monitoring

- One CloudWatch log group per function (`/aws/lambda/replog-api-auth`, `/aws/lambda/replog-api`) with 14-day retention.
- Recommended alarms per function: API Gateway 5xx rate, Lambda error rate, Lambda throttle rate, Lambda duration p99 above 5 s.

## Health Checks

- `GET /api/auth/health` → replog-api-auth
- `GET /api/sync/health` → replog-api

Each runs `DescribeTable` on the workouts table to verify DynamoDB connectivity. Suitable as HTTP health probes from any uptime monitor.

## Cookie Flow

`Amazon.Lambda.AspNetCoreServer.Hosting` translates ASP.NET `Set-Cookie` response headers into the API Gateway v2 `cookies[]` array automatically. The browser receives standard `Set-Cookie` headers — no client change.

## Rate Limiting

The ASP.NET `FixedWindowRateLimiter` (`RequireRateLimiting("sync")`) runs inside the sync Lambda. With Lambda concurrency, the limiter state is per-instance, so the effective limit is `PermitLimit × concurrent-instances`. To enforce a true global per-user limit, move rate limiting to API Gateway or to a DynamoDB-based counter — but for low-traffic single-user usage the current behavior is acceptable.
