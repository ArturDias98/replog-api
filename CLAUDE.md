# Replog API

Workout logging REST API built with .NET 9 following Clean Architecture.

## Solution Structure

```
replog-api.sln
├── replog-api/            # ASP.NET Web API — controllers, middleware, DI setup
├── replog-shared/         # Shared models: API response DTOs, common enums/constants
├── replog-application/    # Business logic layer: service/repository abstractions, CQRS commands & queries
└── replog-infrastructure/ # Implementations: database access, external services, repositories
```

### Project Responsibilities

- **replog-api**: Entry point. Registers services, configures middleware, defines endpoints. References `replog-application` and `replog-infrastructure`.
- **replog-shared**: Lightweight library with no project dependencies. Contains models shared across projects (API response/request DTOs, enums). Referenced by all other projects.
- **replog-application**: Defines interfaces (`IService`, `IRepository`), command/query models (CQRS pattern) for creating and reading data. References `replog-shared`. No infrastructure dependencies.
- **replog-infrastructure**: Implements `replog-application` interfaces. Contains EF Core DbContext, repository implementations, external service clients. References `replog-application` and `replog-shared`.

### Dependency Flow

```
replog-api → replog-infrastructure → replog-application → replog-shared
```

No reverse dependencies. Application layer must never reference Infrastructure or API.

## Tech Stack

- .NET 9 / C# 13
- ASP.NET Core Web API (minimal APIs or controllers)
- OpenAPI enabled

## Build & Run

```bash
dotnet build
dotnet run --project replog-api
```

## Conventions

- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- Root namespace: `replog_api` (for the API project)
- CQRS pattern: commands for writes, queries for reads — defined in `replog-application`
- Interfaces in `replog-application`, implementations in `replog-infrastructure`
