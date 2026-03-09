# Replog API

Workout logging REST API built with .NET 9 following Clean Architecture.

## Project Documentation

The project documentation (backend API spec, sync strategy, etc.) lives in a separate repository: [replog-docs](https://github.com/ArturDias98/replog-docs).
When you need project requirements, API contracts, or sync behavior details, fetch the relevant files from that repository using `gh` or `WebFetch`.

## Solution Structure

```text
replog-api.sln
├── replog-api/                          # ASP.NET Web API — endpoints, middleware, DI setup
│   ├── Auth/                            # UserClaimsExtensions (GetUserId)
│   ├── Endpoints/                       # SyncEndpoints (push/pull)
│   ├── Middleware/                       # ValidationExceptionHandler
│   └── Program.cs                       # App configuration entry point
│
├── replog-domain/                       # Domain entities (no dependencies)
│   └── Entities/                        # WorkoutEntity, MuscleGroupEntity, ExerciseEntity, LogEntity
│
├── replog-shared/                       # Shared models (no project dependencies)
│   ├── Enums/                           # EntityType, ChangeAction
│   ├── Json/                            # JsonDefaults
│   └── Models/
│       ├── Requests/                    # PushSyncRequest, SyncChangeDto
│       ├── Responses/                   # PushSyncResponse, PullSyncResponse, WorkoutDto, ErrorResponse
│       └── Sync/                        # Add/Update/Delete sync models per entity
│
├── replog-application/                  # Business logic — CQRS, interfaces, validators
│   ├── Commands/
│   │   ├── PushSyncCommand.cs
│   │   └── Handlers/
│   │       ├── PushSyncCommandHandler.cs
│   │       └── Processors/              # WorkoutChangeProcessor, MuscleGroupChangeProcessor,
│   │                                    # ExerciseChangeProcessor, LogChangeProcessor, ChangeDataHelper
│   ├── Queries/
│   │   ├── PullSyncQuery.cs
│   │   └── Handlers/PullSyncQueryHandler.cs
│   ├── Interfaces/
│   │   ├── IWorkoutRepository.cs
│   │   └── SyncOperations/              # IWorkoutSyncRepository, IMuscleGroupSyncRepository,
│   │                                    # IExerciseSyncRepository, ILogSyncRepository
│   ├── Mappers/WorkoutMapper.cs
│   ├── Validators/                      # FluentValidation validators per entity per action
│   └── DependencyInjection.cs
│
├── replog-infrastructure/               # DynamoDB implementations
│   ├── Repositories/
│   │   ├── WorkoutRepository.cs
│   │   └── SyncOperations/              # BaseSyncRepository, WorkoutSyncRepository,
│   │                                    # MuscleGroupSyncRepository, ExerciseSyncRepository,
│   │                                    # LogSyncRepository
│   └── DependencyInjection.cs
│
├── replog-application.tests/            # Application layer tests (xUnit)
│   ├── Fixtures/                        # ApplicationFixture, ApplicationCollection
│   ├── Handlers/                        # PushSync and PullSync handler tests
│   └── Helpers/                         # SyncChangeBuilder
│
├── replog-infrastructure.tests/         # Infrastructure layer tests (xUnit + Testcontainers)
│   ├── Fixtures/                        # DynamoDbCollection
│   └── Repositories/                    # SyncOperationRepositoryTests, WorkoutRepositoryTests
│
└── replog-tests-shared/                 # Shared test utilities
    ├── Comparers/                       # Entity comparers + DictionaryCompareHelper
    └── Fixtures/                        # DynamoDbFixture (Testcontainers setup)
```

### Project Responsibilities

- **replog-api**: Entry point. Registers services, configures middleware, defines endpoints. References `replog-application` and `replog-infrastructure`.
- **replog-domain**: Domain entities (`WorkoutEntity`, `MuscleGroupEntity`, `ExerciseEntity`, `LogEntity`). No project dependencies.
- **replog-shared**: Lightweight library with no project dependencies. Contains models shared across projects (API response/request DTOs, sync models, enums). Referenced by all other projects.
- **replog-application**: Defines interfaces (`IRepository`), command/query models (CQRS pattern), validators, and change processors. References `replog-domain` and `replog-shared`. No infrastructure dependencies.
- **replog-infrastructure**: Implements `replog-application` interfaces. Contains DynamoDB repository implementations. References `replog-application` and `replog-shared`.
- **replog-application.tests**: Tests for CQRS handlers, validators, and change processors.
- **replog-infrastructure.tests**: Integration tests for DynamoDB repositories using Testcontainers.
- **replog-tests-shared**: Shared test fixtures (DynamoDbFixture) and entity comparers.

### Dependency Flow

```text
replog-domain    (no dependencies)
replog-shared    (no dependencies)
replog-application → replog-domain, replog-shared
replog-infrastructure → replog-application, replog-shared
replog-api → replog-infrastructure, replog-application, replog-shared
```

No reverse dependencies. Application layer must never reference Infrastructure or API.

## Tech Stack

- .NET 9 / C# 13
- ASP.NET Core Web API (minimal APIs)
- Amazon DynamoDB (via AWSSDK.DynamoDBv2)
- FluentValidation for input validation
- Google JWT Bearer authentication
- OpenAPI enabled
- xUnit + Testcontainers for testing
- Docker Compose for local DynamoDB

## Build & Run

```bash
# Start local DynamoDB
docker compose up -d

# Build
dotnet build

# Run the API
dotnet run --project replog-api
```

## Testing

**RULE: After every code modification, run `dotnet test` to verify nothing is broken.**

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test replog-application.tests
dotnet test replog-infrastructure.tests
```

- Infrastructure tests use **Testcontainers** — Docker must be running.
- Application tests use mocked dependencies via DI fixture.
- Shared comparers in `replog-tests-shared` for entity equality assertions.
- Tests follow the pattern: `MethodName_ShouldExpectedBehavior_WhenCondition`.

## Security

- **Authentication**: Google OAuth JWT Bearer tokens. All endpoints require authorization.
- **Ownership validation**: Every repository write operation (create, update, delete) must include `userId` in the DynamoDB `ConditionExpression` to enforce data ownership. A user must never be able to read or modify another user's data.
- **Rate limiting**: Fixed window per user (configurable via `RateLimiter:PermitLimit` in appsettings).
- **CORS**: Restricted to `localhost:4200` (dev) and `replog.adrvcode.com` (prod). Only `GET` and `POST` methods allowed.
- **Input validation**: FluentValidation on all incoming sync requests. Max 100 changes per push.

## Conventions

- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- Root namespace: `replog_api` (for the API project)
- CQRS pattern: commands for writes, queries for reads — defined in `replog-application`
- Interfaces in `replog-application`, implementations in `replog-infrastructure`
- Entities in `replog-domain/Entities/`, DTOs and sync models in `replog-shared/Models/`
- One FluentValidation validator per entity per action (Add/Update/Delete) in `replog-application/Validators/`
- One change processor per entity type in `replog-application/Commands/Handlers/Processors/`
- All DynamoDB sync repositories extend `BaseSyncRepository`
- Entity hierarchy: Workout → MuscleGroup → Exercise → Log (nested DynamoDB document)

## Key Files

| Purpose | Path |
| --- | --- |
| App entry point | `replog-api/Program.cs` |
| Endpoint definitions | `replog-api/Endpoints/SyncEndpoints.cs` |
| User ID extraction | `replog-api/Auth/UserClaimsExtensions.cs` |
| Push command handler | `replog-application/Commands/Handlers/PushSyncCommandHandler.cs` |
| Pull query handler | `replog-application/Queries/Handlers/PullSyncQueryHandler.cs` |
| DI (application) | `replog-application/DependencyInjection.cs` |
| DI (infrastructure) | `replog-infrastructure/DependencyInjection.cs` |
| Base repository | `replog-infrastructure/Repositories/SyncOperations/BaseSyncRepository.cs` |
| Docker DynamoDB | `docker-compose.yml` |
| Test DynamoDB fixture | `replog-tests-shared/Fixtures/DynamoDbFixture.cs` |
