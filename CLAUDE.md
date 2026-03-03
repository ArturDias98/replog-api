# Replog API

Workout logging REST API built with .NET 9 following Clean Architecture.

## Project Documentation

The project documentation (backend API spec, sync strategy, etc.) lives in a separate repository: [replog-docs](https://github.com/ArturDias98/replog-docs).
When you need project requirements, API contracts, or sync behavior details, fetch the relevant files from that repository using `gh` or `WebFetch`.

## Solution Structure

```
replog-api.sln
├── replog-api/            # ASP.NET Web API — controllers, middleware, DI setup
├── replog-domain/         # Domain entities (no dependencies)
├── replog-shared/         # Shared models: API response DTOs, request models, enums, JSON utils
├── replog-application/    # Business logic layer: service/repository abstractions, CQRS commands & queries
└── replog-infrastructure/ # Implementations: database access, external services, repositories
```

### Project Responsibilities

- **replog-api**: Entry point. Registers services, configures middleware, defines endpoints. References `replog-application` and `replog-infrastructure`.
- **replog-domain**: Domain entities (`WorkoutEntity`, `MuscleGroupEntity`, `ExerciseEntity`, `LogEntity`). No project dependencies.
- **replog-shared**: Lightweight library with no project dependencies. Contains models shared across projects (API response/request DTOs, sync models, enums). Referenced by all other projects.
- **replog-application**: Defines interfaces (`IService`, `IRepository`), command/query models (CQRS pattern) for creating and reading data. References `replog-domain` and `replog-shared`. No infrastructure dependencies.
- **replog-infrastructure**: Implements `replog-application` interfaces. Contains DynamoDB repository implementations and external service clients. References `replog-application` and `replog-shared`.

### Dependency Flow

```
replog-domain    (no dependencies)
replog-shared    (no dependencies)
replog-application → replog-domain, replog-shared
replog-infrastructure → replog-application, replog-shared
replog-api → replog-infrastructure, replog-application, replog-shared
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
