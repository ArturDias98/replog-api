# Replog API

Workout logging REST API built with .NET 9 following Clean Architecture.

## Project Documentation

API documentation lives in the `docs/` directory of this repository:

- `docs/backend-api.md` — sync API spec (endpoints, request/response shapes, entity processing, conflict resolution)
- `docs/authentication.md` — auth flow, token details, error codes

Read these files directly when you need API contracts or sync behavior details.

For web application context (client-side code, front-end docs), fetch from: `https://github.com/ArturDias98/replog/tree/main/code/docs`

## Solution Structure

```text
replog-api.sln
├── replog-api/                          # Sync Lambda host — push/pull/health endpoints
│   ├── Endpoints/                       # SyncEndpoints, HealthEndpoints (delegated to host lib)
│   └── Program.cs                       # App configuration entry point
│
├── replog-api-auth/                     # Auth Lambda host — login/refresh/logout/health
│   ├── Auth/                            # AuthService, TokenService, GoogleTokenValidator
│   ├── Endpoints/                       # AuthEndpoints
│   ├── Settings/                        # GoogleAuthSettings
│   └── Program.cs
│
├── replog-api-host/                     # Shared web-host bootstrap (class library)
│   ├── Auth/                            # UserClaimsExtensions
│   ├── Endpoints/                       # HealthEndpointExtensions (parameterised by path)
│   ├── Middleware/                      # GlobalExceptionHandler
│   ├── Settings/                        # JwtSettings
│   ├── CorsExtensions.cs                # AddReplogCors
│   ├── JwtAuthExtensions.cs             # AddReplogJwtBearer (cookie → bearer)
│   └── SecretsLoader.cs                 # AWS Secrets Manager → IConfiguration at cold start
│
├── replog-domain/                       # Domain entities (no dependencies)
│   └── Entities/                        # WorkoutEntity, MuscleGroupEntity, ExerciseEntity, LogEntity
│
├── replog-shared/                       # Shared models (no project dependencies)
│   ├── Enums/                           # EntityType, ChangeAction
│   ├── Json/                            # JsonDefaults
│   └── Models/
│       ├── Requests/                    # PushSyncRequest, SyncChangeDto, LoginRequest, RefreshTokenRequest
│       ├── Responses/                   # PushSyncResponse, PullSyncResponse, WorkoutDto, AuthResponse, ErrorResponse
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
│   ├── Result.cs                        # Result<T> pattern for explicit failure handling
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
├── replog-api.tests/                    # Sync host integration tests (xUnit + WebApplicationFactory)
│   ├── Fixtures/                        # ApiWebApplicationFactory, ApiCollection
│   └── Endpoints/                       # SyncEndpointTests, HealthEndpointTests
│
├── replog-api-auth.tests/               # Auth host integration tests (xUnit + WebApplicationFactory)
│   ├── Fixtures/                        # AuthApiWebApplicationFactory, AuthApiCollection
│   ├── Endpoints/                       # AuthEndpointTests, HealthEndpointTests
│   └── Handlers/                        # LoginServiceTests, RefreshTokenServiceTests
│
└── replog-tests-shared/                 # Shared test utilities
    ├── Comparers/                       # Entity comparers + DictionaryCompareHelper
    └── Fixtures/                        # DynamoDbFixture (Testcontainers setup)
```

### Project Responsibilities

- **replog-api**: Sync Lambda host. Hosts `/api/sync/*` and `/api/sync/health`. References `replog-api-host`, `replog-application`, `replog-infrastructure`.
- **replog-api-auth**: Auth Lambda host. Hosts `/api/auth/*` and `/api/auth/health`. References `replog-api-host`, `replog-application` (for `IUserRepository`), `replog-infrastructure`.
- **replog-api-host**: Shared web-host bootstrap library. JWT bearer setup, CORS, secrets loader, exception middleware, parameterised health endpoint, JWT settings. Both Lambda hosts depend on it.
- **replog-domain**: Domain entities (`WorkoutEntity`, `MuscleGroupEntity`, `ExerciseEntity`, `LogEntity`, `UserEntity`). No project dependencies.
- **replog-shared**: Lightweight library with no project dependencies. Contains models shared across projects (API response/request DTOs, sync models, enums). Referenced by all other projects.
- **replog-application**: Defines interfaces (`IRepository`), command/query models (CQRS pattern), validators, and change processors. References `replog-domain` and `replog-shared`. No infrastructure dependencies.
- **replog-infrastructure**: Implements `replog-application` interfaces. Contains DynamoDB repository implementations. References `replog-application` and `replog-shared`.
- **replog-application.tests**: Tests for CQRS handlers, validators, and change processors.
- **replog-infrastructure.tests**: Integration tests for DynamoDB repositories using Testcontainers.
- **replog-api.tests**: HTTP-level integration tests for the sync host using `WebApplicationFactory<Program>`.
- **replog-api-auth.tests**: HTTP-level integration tests for the auth host plus AuthService unit tests.
- **replog-tests-shared**: Shared test fixtures (DynamoDbFixture) and entity comparers.

### Dependency Flow

```text
replog-domain        (no dependencies)
replog-shared        (no dependencies)
replog-application   → replog-domain, replog-shared
replog-infrastructure → replog-application, replog-shared
replog-api-host      → replog-infrastructure, replog-shared
replog-api           → replog-api-host, replog-application, replog-infrastructure, replog-shared
replog-api-auth      → replog-api-host, replog-application, replog-infrastructure, replog-shared
```

No reverse dependencies. Application layer must never reference Infrastructure or any host.

## Tech Stack

- .NET 9 / C# 13
- ASP.NET Core Web API (minimal APIs)
- Amazon DynamoDB (via AWSSDK.DynamoDBv2)
- FluentValidation for input validation
- Google OAuth → custom JWT (HS256) authentication
- OpenAPI enabled
- xUnit + Testcontainers for testing
- Docker Compose for local DynamoDB

## Build & Run

```bash
# Start local DynamoDB
docker compose up -d

# Build
dotnet build

# Run the sync API host
dotnet run --project replog-api

# Run the auth API host
dotnet run --project replog-api-auth
```

## Testing

**RULE: After every code modification, run `dotnet test` to verify nothing is broken.**

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test replog-application.tests
dotnet test replog-infrastructure.tests
dotnet test replog-api.tests
dotnet test replog-api-auth.tests
```

- Infrastructure tests use **Testcontainers** — Docker must be running.
- `replog-api.tests` and `replog-api-auth.tests` use **WebApplicationFactory** + Testcontainers — Docker must be running.
- Application tests use mocked dependencies via DI fixture.
- Shared comparers in `replog-tests-shared` for entity equality assertions.
- Tests follow the pattern: `MethodName_ShouldExpectedBehavior_WhenCondition`.

## Documentation

**RULE: After any change to endpoints, request/response models, error codes, auth behavior, or security config, update `docs/backend-api.md` and/or `docs/authentication.md` to match the current implementation.**

## Security

- **Authentication**: Custom JWT (HS256) issued by the **auth Lambda** (`replog-api-auth`) after validating a Google ID token. The **sync Lambda** (`replog-api`) verifies that JWT locally on every `/api/sync/*` request — both Lambdas share the same signing secret. Protected endpoints require the token in the `access_token` cookie (or `Authorization: Bearer <token>` header). Auth endpoints (`/api/auth/*`) are public.
- **Ownership validation**: Every repository write operation (create, update, delete) must include `userId` in the DynamoDB `ConditionExpression` to enforce data ownership. A user must never be able to read or modify another user's data.
- **Rate limiting**: Fixed window per user (configurable via `RateLimiter:PermitLimit` in appsettings).
- **CORS**: Restricted to `localhost:4200` (dev) and `replog.adrvcode.com` (prod). Only `GET` and `POST` methods allowed.
- **Input validation**: FluentValidation on all incoming sync requests. Max 100 changes per push.

## Conventions

- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- Root namespaces: `replog_api` (sync host), `replog_api_auth` (auth host), `replog_api_host` (shared host lib)
- CQRS pattern: commands for writes, queries for reads — defined in `replog-application`
- `Result<T>` pattern for handlers with explicit failure paths (e.g. `PushSyncCommandHandler`). Handlers that only read data return `T` directly and let exceptions propagate to `GlobalExceptionHandler`.
- Interfaces in `replog-application`, implementations in `replog-infrastructure`
- Entities in `replog-domain/Entities/`, DTOs and sync models in `replog-shared/Models/`
- One FluentValidation validator per entity per action (Add/Update/Delete) in `replog-application/Validators/`
- One change processor per entity type in `replog-application/Commands/Handlers/Processors/`
- All DynamoDB sync repositories extend `BaseSyncRepository`
- Entity hierarchy: Workout → MuscleGroup → Exercise → Log (nested DynamoDB document)

## Key Files

| Purpose | Path |
| --- | --- |
| Sync host entry point | `replog-api/Program.cs` |
| Auth host entry point | `replog-api-auth/Program.cs` |
| Auth endpoints | `replog-api-auth/Endpoints/AuthEndpoints.cs` |
| Sync endpoints | `replog-api/Endpoints/SyncEndpoints.cs` |
| Shared JWT bearer setup | `replog-api-host/JwtAuthExtensions.cs` |
| Shared CORS setup | `replog-api-host/CorsExtensions.cs` |
| Shared health endpoint | `replog-api-host/Endpoints/HealthEndpointExtensions.cs` |
| Shared exception middleware | `replog-api-host/Middleware/GlobalExceptionHandler.cs` |
| Secrets Manager loader | `replog-api-host/SecretsLoader.cs` |
| User ID extraction | `replog-api-host/Auth/UserClaimsExtensions.cs` |
| JWT settings | `replog-api-host/Settings/JwtSettings.cs` |
| Auth service | `replog-api-auth/Auth/AuthService.cs` |
| Token service | `replog-api-auth/Auth/TokenService.cs` |
| Google token validator | `replog-api-auth/Auth/GoogleTokenValidator.cs` |
| Push command handler | `replog-application/Commands/Handlers/PushSyncCommandHandler.cs` |
| Pull query handler | `replog-application/Queries/Handlers/PullSyncQueryHandler.cs` |
| Result pattern | `replog-application/Result.cs` |
| DI (application) | `replog-application/DependencyInjection.cs` |
| DI (infrastructure) | `replog-infrastructure/DependencyInjection.cs` |
| Base repository | `replog-infrastructure/Repositories/SyncOperations/BaseSyncRepository.cs` |
| Docker DynamoDB | `docker-compose.yml` |
| Test DynamoDB fixture | `replog-tests-shared/Fixtures/DynamoDbFixture.cs` |
| Sync API test factory | `replog-api.tests/Fixtures/ApiWebApplicationFactory.cs` |
| Auth API test factory | `replog-api-auth.tests/Fixtures/AuthApiWebApplicationFactory.cs` |
| Lambda deployment guide | `docs/lambda-deployment.md` |
| Backend API spec | `docs/backend-api.md` |
| Auth spec | `docs/authentication.md` |
