# Lambda Deployment Guide

How the `replog-api` ASP.NET Core project is packaged and deployed as a single AWS Lambda function behind API Gateway HTTP API (v2). This document is the contract for the infrastructure repo that owns the CloudFormation stack.

## Architecture

```text
client → API Gateway (HTTP API v2) → Lambda (replog-api) → DynamoDB
                                                         → Google OAuth (for /api/auth/login)
```

The whole ASP.NET Core pipeline (middleware, JWT auth, rate limiter, CORS, all endpoints) runs inside one Lambda. `Amazon.Lambda.AspNetCoreServer.Hosting` translates API Gateway HTTP API events to ASP.NET `HttpContext` and back, so endpoint code is unchanged from the local Kestrel host.

## Local vs Lambda

`Program.cs` calls `builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi)`. This is auto-detecting:

- **Outside Lambda** (the `AWS_LAMBDA_FUNCTION_NAME` env var is absent): no-op. `dotnet run` launches Kestrel as before.
- **Inside Lambda** (env var present): the call replaces the default web server with the Lambda runtime adapter; API Gateway events drive the request pipeline.

No code branching. The same artifact runs everywhere.

## Building the Deployment Artifact

```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet lambda package -pl replog-api -c Release -o bin/Release/replog-api.zip
```

The zip contains the published output and is what the infra repo uploads to S3 / passes to CloudFormation.

## Function Configuration

| Setting | Value |
| --- | --- |
| Runtime | `dotnet8` (managed) |
| Architecture | `x86_64` (or `arm64` for ~20% cheaper / faster) |
| Memory | 1024 MB |
| Timeout | 30 s |
| Handler | `replog-api` (assembly name only — required by `AddAWSLambdaHosting`) |

Defaults match `replog-api/aws-lambda-tools-defaults.json`.

## Required Environment Variables

| Variable | Source | Notes |
| --- | --- | --- |
| `JWT_SECRET_ARN` | plain | ARN of the Secrets Manager secret holding the JWT HS256 signing key. Resolved at cold start by [`SecretsLoader`](../replog-api/Auth/SecretsLoader.cs) and bound to `Jwt:Secret`. |
| `GOOGLE_CLIENT_ID_ARN` | plain | ARN of the Secrets Manager secret holding the Google OAuth client ID. Resolved at cold start and bound to `Google:ClientId`. |
| `Jwt__Issuer` | plain | `replog-api` (default) |
| `Jwt__Audience` | plain | `replog-client` (default) |
| `Jwt__AccessTokenExpirationMinutes` | plain | `15` |
| `Jwt__RefreshTokenExpirationDays` | plain | `30` |
| `Jwt__AccessTokenCookieExpirationDays` | plain | `30` |
| `Jwt__RefreshTokenCookieExpirationDays` | plain | `30` |
| `DynamoDB__Region` | plain | e.g. `us-east-1` |
| `DynamoDB__TableName` | plain | `replog-workouts` |
| `DynamoDB__UsersTableName` | plain | `replog-users` |
| `RateLimiter__PermitLimit` | plain (optional) | per-user requests/minute on `/api/sync/*` |
| `ASPNETCORE_ENVIRONMENT` | plain | `Production` |

Double-underscore (`__`) maps to ASP.NET configuration section delimiter (`Jwt:Secret`, etc.).

Both secrets are stored in AWS Secrets Manager as plain text (`SecretString`). The Lambda fetches them at cold start using its execution-role credentials — so the `SecretsManagerReadPolicy` IAM block in the CloudFormation stack must grant `secretsmanager:GetSecretValue` on both ARNs. Resolution is gated on `ASPNETCORE_ENVIRONMENT=Production`; in any other environment `SecretsLoader` is a no-op and `Jwt:Secret` / `Google:ClientId` come from `appsettings.{Environment}.json` as before. Secret rotation propagates on the next cold start with no redeploy needed.

## API Gateway (HTTP API v2)

Single catch-all integration → Lambda. Recommended route: `ANY /{proxy+}` with `$default` stage.

CORS (must allow credentials so cookies flow):

```yaml
AllowOrigins:   [https://replog.adrvcode.com, http://localhost:4200]
AllowMethods:   [GET, POST]
AllowHeaders:   [Content-Type]
AllowCredentials: true
MaxAge:         600
```

Throttling: stage-level default 100 RPS / burst 200 is a reasonable starting point. Per-user rate limiting is enforced inside the Lambda (`RequireRateLimiting("sync")`), not at API Gateway.

## IAM Permissions for the Lambda Execution Role

- `dynamodb:GetItem`, `PutItem`, `UpdateItem`, `DeleteItem`, `Query`, `BatchWriteItem`, `TransactWriteItems`, `DescribeTable` on the `replog-workouts` table + its `UserIdIndex` GSI
- Same R/W on `replog-users`
- `logs:CreateLogGroup`, `CreateLogStream`, `PutLogEvents` (or use the AWSLambdaBasicExecutionRole managed policy)
- `secretsmanager:GetSecretValue` / `ssm:GetParameter` if secrets are pulled at cold start

## Logging & Monitoring

- One CloudWatch log group per function with 14-day retention.
- Recommended alarms: API Gateway 5xx rate, Lambda error rate, Lambda throttle rate, Lambda duration p99 above 5 s.

## Health Checks

The existing `GET /api/health` endpoint runs `DescribeTable` on the workouts table. Suitable as an HTTP health probe from any uptime monitor.

## Cookie Flow

`Amazon.Lambda.AspNetCoreServer.Hosting` translates ASP.NET `Set-Cookie` response headers into the API Gateway v2 `cookies[]` array automatically. The browser receives standard `Set-Cookie` headers — no client change.

## Rate Limiting

The ASP.NET `FixedWindowRateLimiter` (`RequireRateLimiting("sync")`) runs inside the Lambda. With Lambda concurrency, the limiter state is per-instance, so the effective limit is `PermitLimit × concurrent-instances`. To enforce a true global per-user limit, move rate limiting to API Gateway or to a DynamoDB-based counter — but for low-traffic single-user usage the current behavior is acceptable.
