# Replog API

Workout logging REST API built with .NET 9 and Clean Architecture.

> Work in progress

## Project Documentation

The project documentation (backend API spec, sync strategy, etc.) lives in a separate repository: [replog-docs](https://github.com/ArturDias98/replog-docs).
When you need project requirements, API contracts, or sync behavior details, fetch the relevant files from that repository using `gh` or `WebFetch`.

## Tech Stack

- .NET 9 / C# 13
- ASP.NET Core Minimal APIs
- AWS DynamoDB
- Google JWT Authentication
- FluentValidation
- CQRS pattern

## Solution Structure

```
replog-api.sln
├── replog-api/            # Web API entry point, endpoints, middleware
├── replog-application/    # Business logic, CQRS handlers, validators
├── replog-infrastructure/ # DynamoDB repositories, external services
└── replog-shared/         # DTOs, entities, enums
```

## Build & Run

```bash
dotnet build
dotnet run --project replog-api
```

## API Endpoints

| Method | Route             | Description              |
|--------|-------------------|--------------------------|
| POST   | `/api/sync/push`  | Push local changes       |
| GET    | `/api/sync/pull`  | Pull server state        |

All endpoints require a Google JWT bearer token and are rate-limited to 10 requests/minute per user.
