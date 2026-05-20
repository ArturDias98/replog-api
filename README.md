# Replog API

Workout logging REST API built with .NET 10 and Clean Architecture.

> Work in progress

## Project Documentation

The project documentation (backend API spec, sync strategy, etc.) lives in a separate repository: [replog-docs](https://github.com/ArturDias98/replog-docs).
When you need project requirements, API contracts, or sync behavior details, fetch the relevant files from that repository using `gh` or `WebFetch`.

## Tech Stack

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- AWS DynamoDB
- Google JWT Authentication
- FluentValidation
- CQRS pattern

## Solution Structure

```
replog-api.sln
├── replog-api/            # Sync Lambda host (push/pull/health)
├── replog-api-auth/       # Auth Lambda host (login/refresh/logout/health)
├── replog-api-host/       # Shared web-host bootstrap (JWT, CORS, secrets, exception handler)
├── replog-api-gateway/    # Local-dev YARP reverse proxy (not deployed)
├── replog-application/    # Business logic, CQRS handlers, validators
├── replog-infrastructure/ # DynamoDB repositories, external services
├── replog-domain/         # Domain entities
└── replog-shared/         # DTOs, sync models, enums
```

## Build & Run

```bash
dotnet build

# Run the sync host (port 5139)
dotnet run --project replog-api

# Run the auth host (port 5140)
dotnet run --project replog-api-auth

# Run the local-dev gateway (port 5000) — single ingress that proxies /api/auth/* and /api/sync/*
dotnet run --project replog-api-gateway
```

In production, API Gateway HTTP API does this routing. Locally, `replog-api-gateway` is a small YARP proxy so the web app can use a single `apiBaseUrl=http://localhost:5000` in dev.

## API Endpoints

| Method | Route                | Lambda           | Description                      |
|--------|----------------------|------------------|----------------------------------|
| POST   | `/api/auth/login`    | replog-api-auth  | Exchange Google ID token for JWT |
| POST   | `/api/auth/refresh`  | replog-api-auth  | Rotate access + refresh tokens   |
| POST   | `/api/auth/logout`   | replog-api-auth  | Clear auth cookies               |
| GET    | `/api/auth/health`   | replog-api-auth  | Auth Lambda health probe         |
| POST   | `/api/sync/push`     | replog-api       | Push local changes               |
| GET    | `/api/sync/pull`     | replog-api       | Pull server state                |
| GET    | `/api/sync/health`   | replog-api       | Sync Lambda health probe         |

`/api/sync/*` requires a JWT (in the `access_token` cookie or `Authorization: Bearer …` header) and is rate-limited per user.
