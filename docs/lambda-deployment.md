# Lambda Deployment Guide

How the replog backend is packaged and deployed as three AWS Lambda functions behind one API Gateway HTTP API (v2). This document is the contract for the infrastructure repo that owns the CloudFormation stack.

## Architecture

```text
client → API Gateway (HTTP API v2)
           ├── /api/auth/*   → Lambda (replog-api-auth) → DynamoDB
           │                                            → Google OAuth (login)
           └── /api/sync/*   → [REQUEST authorizer: replog-api-authorizer]
                             → Lambda (replog-api)      → DynamoDB
```

The auth and sync Lambdas boot an ASP.NET Core pipeline via `Amazon.Lambda.AspNetCoreServer.Hosting`. Shared **web** bootstrap (CORS, exception handler, health endpoint, `GetUserId`) lives in the auth-free `replog-api-host` class library; shared **auth** primitives (`JwtSettings`, `AccessTokenValidator`) live in the lean `replog-api-auth-core` library used by the auth host, the authorizer, and the dev gateway.

The auth Lambda issues JWTs at `/api/auth/login` and `/api/auth/refresh`. Requests to `/api/sync/*` are authenticated **at the gateway** by the `replog-api-authorizer` Lambda (a REQUEST authorizer): it validates the `access_token` cookie with the shared HS256 secret and returns the user id as authorizer context. API Gateway maps that id into an `overwrite:header.x-user-id` request parameter, and the sync Lambda simply trusts that header — it performs **no token validation** and holds **no JWT secret**. Health routes (`/api/*/health`) and the public auth routes are not behind the authorizer.

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

# Authorizer Lambda
dotnet lambda package -pl replog-api-authorizer -c Release -o bin/Release/replog-api-authorizer.zip
```

CI does all three via a matrix over `[replog-api, replog-api-auth, replog-api-authorizer]` in `.github/workflows/deploy.yml`.

## Function Configuration

| Setting | replog-api / replog-api-auth | replog-api-authorizer |
| --- | --- | --- |
| Runtime | `dotnet10` (managed) | `dotnet10` (managed) |
| Architecture | `x86_64` | `x86_64` |
| Memory | 1024 MB | 512 MB |
| Timeout | 30 s | 10 s |
| Handler | assembly name (required by `AddAWSLambdaHosting`) | `replog-api-authorizer::replog_api_authorizer.Function::FunctionHandler` |

The authorizer is a plain class-library Lambda (not ASP.NET), so its handler is the full `Assembly::Type::Method` form. Defaults live in each project's `aws-lambda-tools-defaults.json`.

## Required Environment Variables

| Variable | replog-api-auth | replog-api-authorizer | replog-api (sync) | Notes |
| --- | --- | --- | --- | --- |
| `JWT_SECRET_ARN` | required | required | — | ARN of the Secrets Manager secret holding the JWT HS256 signing key. Auth signs; the authorizer verifies. The **sync Lambda no longer needs it** — auth happens at the gateway. |
| `GOOGLE_CLIENT_ID_ARN` | required | — | — | ARN of the Google OAuth client-id secret. Auth-only. Bound to `Google:ClientId`. |
| `Jwt__Issuer` | `replog-api` | `replog-api` | — | default |
| `Jwt__Audience` | `replog-client` | `replog-client` | — | default |
| `Jwt__AccessTokenExpirationMinutes` | `15` | — | — | auth issues tokens |
| `Jwt__RefreshTokenExpirationDays` | `30` | — | — | auth issues tokens |
| `Jwt__AccessTokenCookieExpirationDays` | `30` | — | — | auth issues tokens |
| `Jwt__RefreshTokenCookieExpirationDays` | `30` | — | — | auth issues tokens |
| `DynamoDB__Region` | required | — | required | e.g. `us-east-1` |
| `DynamoDB__TableName` | required | — | required | `replog-workouts` |
| `DynamoDB__UsersTableName` | required | — | required | `replog-users` |
| `RateLimiter__PermitLimit` | — | — | optional | per-user requests/minute on `/api/sync/*` |
| `ASPNETCORE_ENVIRONMENT` | `Production` | — | `Production` | gates the auth host's Secrets Manager fetch |

Double-underscore (`__`) maps to the ASP.NET configuration section delimiter (`Jwt:Secret`, etc.).

Secrets are stored in AWS Secrets Manager as plain text (`SecretString`). The auth Lambda resolves them at cold start via [`SecretsLoader`](../replog-api-auth/Auth/SecretsLoader.cs) (gated on `ASPNETCORE_ENVIRONMENT=Production`); the authorizer resolves `JWT_SECRET_ARN` directly at cold start. The `SecretsManagerReadPolicy` IAM block must grant `secretsmanager:GetSecretValue` on the ARNs each function needs (auth: both; authorizer: `JWT_SECRET_ARN`; **sync: none**). Secret rotation propagates on the next cold start with no redeploy needed.

## API Gateway (HTTP API v2) Routing

Two integrations — one per ASP.NET Lambda. Routes:

| Route | Integration | Authorizer |
| --- | --- | --- |
| `ANY /api/auth/{proxy+}` | replog-api-auth | none (public) |
| `GET /api/auth/health` | replog-api-auth | none (public) |
| `ANY /api/sync/{proxy+}` | replog-api | **replog-sync-authorizer** (CUSTOM) |
| `GET /api/sync/health` | replog-api | none (public) |

The `replog-sync-authorizer` is an `AWS::ApiGatewayV2::Authorizer` of type `REQUEST` (`AuthorizerPayloadFormatVersion: 2.0`, `EnableSimpleResponses: true`, `IdentitySource: $request.header.Cookie`, `AuthorizerResultTtlInSeconds: 0`). The sync integration carries `RequestParameters: { "overwrite:header.x-user-id": "$context.authorizer.userId" }`. Each integration and the authorizer need a `lambda:InvokeFunction` permission for `apigateway.amazonaws.com`.

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

All three functions currently share one execution role for simplicity. Splitting into least-privilege roles is a future tightening (the authorizer needs only `secretsmanager:GetSecretValue` on `JWT_SECRET_ARN` + logs; the sync function needs only DynamoDB + logs and **no** Secrets Manager access).

DynamoDB R/W (auth + sync):

- `dynamodb:GetItem`, `PutItem`, `UpdateItem`, `DeleteItem`, `Query`, `BatchWriteItem`, `TransactWriteItems`, `DescribeTable` on the `replog-workouts` table + its `UserIdIndex` GSI
- Same R/W on `replog-users`

Logs (all three):

- `logs:CreateLogGroup`, `CreateLogStream`, `PutLogEvents` (or the AWSLambdaBasicExecutionRole managed policy)

Secrets Manager:

- `secretsmanager:GetSecretValue` on `JWT_SECRET_ARN` (auth + authorizer)
- `secretsmanager:GetSecretValue` on `GOOGLE_CLIENT_ID_ARN` (auth only)

## Logging & Monitoring

- One CloudWatch log group per function (`/aws/lambda/replog-api-auth`, `/aws/lambda/replog-api`, `/aws/lambda/replog-api-authorizer`) with 14-day retention.
- Recommended alarms per function: API Gateway 5xx rate, Lambda error rate, Lambda throttle rate, Lambda duration p99 above 5 s.

## Health Checks

- `GET /api/auth/health` → replog-api-auth
- `GET /api/sync/health` → replog-api

Each runs `DescribeTable` on the workouts table to verify DynamoDB connectivity. Suitable as HTTP health probes from any uptime monitor.

## Cookie Flow

`Amazon.Lambda.AspNetCoreServer.Hosting` translates ASP.NET `Set-Cookie` response headers into the API Gateway v2 `cookies[]` array automatically. The browser receives standard `Set-Cookie` headers — no client change.

## Rate Limiting

The ASP.NET `FixedWindowRateLimiter` (`RequireRateLimiting("sync")`) runs inside the sync Lambda. With Lambda concurrency, the limiter state is per-instance, so the effective limit is `PermitLimit × concurrent-instances`. To enforce a true global per-user limit, move rate limiting to API Gateway or to a DynamoDB-based counter — but for low-traffic single-user usage the current behavior is acceptable.
