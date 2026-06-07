# Replog API

Workout logging sync API built with .NET 10 and Clean Architecture.

> Work in progress

## Related Repositories

- **[replog-auth](https://github.com/ArturDias98/replog-auth)** — Auth Lambda (login, refresh, logout)
- **[replog](https://github.com/ArturDias98/replog)** — Web client

## Tech Stack

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- AWS DynamoDB
- FluentValidation
- CQRS pattern

## Solution Structure

```
replog-api.sln
├── replog-api/            # Sync Lambda host (push/pull/health)
├── replog-api-auth/       # Auth Lambda host — also maintained as a standalone repo
├── replog-api-auth-core/  # Shared JWT primitives (JwtSettings, AccessTokenValidator)
├── replog-api-authorizer/ # API Gateway REQUEST authorizer Lambda
├── replog-api-host/       # Shared web-host bootstrap (CORS, exception handler, health)
├── replog-api-gateway/    # Local-dev YARP reverse proxy (not deployed)
├── replog-application/    # Business logic, CQRS handlers, validators
├── replog-infrastructure/ # DynamoDB repositories
├── replog-domain/         # Domain entities
└── replog-shared/         # DTOs, sync models, enums
```

## Build & Run

```bash
dotnet build

# Run the sync host (port 5139)
dotnet run --project replog-api

# Run the local-dev gateway (port 5000) — single ingress that proxies /api/auth/* and /api/sync/*
dotnet run --project replog-api-gateway
```

In production, API Gateway HTTP API handles routing. Locally, `replog-api-gateway` is a small YARP proxy so the web app can use a single `apiBaseUrl=http://localhost:5000` in dev.

## API Endpoints

| Method | Route              | Description              |
|--------|--------------------|--------------------------|
| POST   | `/api/sync/push`   | Push local changes       |
| GET    | `/api/sync/pull`   | Pull server state        |
| GET    | `/api/sync/health` | Sync Lambda health probe |

`/api/sync/*` requires a valid `access_token` cookie (issued by the auth Lambda) and is rate-limited per user.

For auth endpoints (`/api/auth/*`) see [replog-api-auth](https://github.com/ArturDias98/replog-api-auth).
