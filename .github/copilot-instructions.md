## Quick orientation for AI code agents

This repository is an ASP.NET minimal API **template** (features-as-classes pattern) targeting .NET 10 with PostgreSQL, Redis, and gRPC. The goal of this document is to give an AI agent the minimal, concrete facts needed to be productive here.

**Note:** This is a template project. Replace `projectname` with your actual project name when setting up a new project via `setup-template.ps1`.

---

### Solution structure (7 projects)

```
projectname.Host    ← Entry point, middleware pipeline, DI composition root
projectname.Tests   ← xUnit integration tests with WebApplicationFactory
Core                    ← Features (endpoints), business layer DI
DA                      ← Data access (EF Core, Repository, UnitOfWork, DbSeeder)
Utility                 ← Shared helpers (Auth/JWT/AES, Email, Logging, gRPC, Validators)
Infrastructure          ← External service integrations (Redis session management, S3)
Auth                    ← Auth project scaffold (JWT, TOTP, AES — migration in progress)
```

### Key facts (first things to know)

- Entry point: `projectname.Host/Program.cs` — calls `ValidateRequiredEnvironmentVariables()`, then `builder.Services.RegisterService(builder.Configuration)` and `await app.Configure()`.
- Service registration: `projectname.Host/Extensions/Resources.cs` (extension `RegisterService`) wires all services: Redis, Auth/JWT, CORS, Rate Limiting, Email, gRPC, OpenAPI, Health Checks, API Versioning, Response Compression, Resilience.
- Endpoint pattern: endpoints are classes implementing `Utility/EndpointController/IFeature.cs` (single method: `void Map(IEndpointRouteBuilder app)`). All feature classes are registered by assembly scan in `Core/Endpoints/RegisterFeatures.cs` and then mapped in `projectname.Host/Extensions/ConfigureApp.cs` with `app.MapEndpoints()`.

### Middleware pipeline order (ConfigureApp.cs)

The pipeline is **order-sensitive**. The correct order is:

```
1. UseCors()                                    — preflight OPTIONS handling
2. Security Headers middleware (inline)          — X-Content-Type-Options, X-Frame-Options, HSTS, etc.
3. UseResponseCompression()                      — Brotli + Gzip response compression
4. UseMiddleware<CorrelationIdMiddleware>         — X-Correlation-ID header + Serilog LogContext
5. UseMiddleware<GlobalExceptionHandlerMiddleware>  — catches all errors
6. UseHttpsRedirection()                         — HTTP → HTTPS redirect
7. UseAuthentication()                           — JWT validation
8. UseAuthorization()                            — policy evaluation
9. UseMiddleware<UserContextMiddleware>           — populates IUserContext from claims
10. UseRateLimiter()                              — 100 req/min per IP (fixed window)
11. GrpcServices()                                — gRPC service mapping
12. MapEndpoints()                               — LAST — terminates the pipeline
```

**⚠️ MapEndpoints() must always be LAST.** Middlewares registered after `MapEndpoints()` will never execute for matched routes.

### Environment variables (fail-fast)

The application **fails fast at startup** if required env vars are missing. `ValidateRequiredEnvironmentVariables()` in `Program.cs` checks for:

| Variable | Purpose |
|---|---|
| `JWT_KEY` | Symmetric key for JWT signing |
| `JWT_ISSUER` | JWT token issuer |
| `JWT_AUDIENCE` | JWT token audience |
| `DBHost` | PostgreSQL connection string |
| `SenderEmail` | SMTP sender email |
| `SenderPassword` | SMTP sender password |
| `AES_KEY` | Base64-encoded AES encryption key |
| `ALLOWED_ORIGINS` | Comma-separated CORS origins |

Additional optional env vars: `RedisHost`, `SMTP_HOST`, `SMTP_PORT`, `SCALAR_BEARER_TOKEN`, `SLACK_WEBHOOK_URL`, `SERVER_URL`, `SEQ_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, `OTEL_SERVICE_VERSION`, `SEED_DATABASE`, `DEFAULT_API_VERSION`, `COMPRESSION_LEVEL`, `HEALTH_CHECK_PATH`, `HEALTH_READY_PATH`, `HEALTH_LIVE_PATH`, `AWS_*`, `S3_BUCKET_NAME`.

All env vars are defined in `projectname.Host/Properties/launchSettings.json`. **No hardcoded credential fallbacks are allowed** — every secret read must use `ArgumentException.ThrowIfNullOrWhiteSpace()`.

### Security features

- **JWT validation**: Issuer, Audience, Lifetime, and IssuerSigningKey are all validated. `ClockSkew = TimeSpan.Zero` (no 5-min tolerance).
- **CORS**: Restricted to explicit origins from `ALLOWED_ORIGINS` env var with `AllowCredentials()`. No `AllowAnyOrigin`.
- **Rate limiting**: 100 requests/minute per IP address (fixed window). Returns 429 on exceeded.
- **Security headers**: Every response includes `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security`, `Referrer-Policy`, `Permissions-Policy`, `X-XSS-Protection`.
- **AES encryption**: Uses random IV per operation (prepended to ciphertext). Key from `AES_KEY` env var (Base64).
- **UserPayload validation**: `IsValid()` checks `UserId` and `UserType` are non-null. Middleware returns 401 if claims are incomplete.

### Where to add code

- **New HTTP endpoints**: Create a class under `Core/Features/...` implementing `IFeature` and implement `Map(...)`. Example: `Core/Features/Example/ExampleFeatures.cs`.
- **Dependency injection / services**: Add registrations in a relevant `DependencyInjection.cs` (examples: `DA/DependencyInjection.cs`, `Core/DependencyInjection.cs`, `Infrastructure/Redis/DependencyInjection.cs`). The host composition is done in `RegisterService` (see `Resources.cs`).
- **Data access**: Repository & unit-of-work live under `DA/Persistence` (`UnitOfWork.cs` and `Repository` pattern). Use `IUnitOfWork` in endpoint handlers. Specification pattern available via `DA/Specifications/BaseSpecification.cs`.
- **Infrastructure concerns**: Redis session management in `Infrastructure/Redis/`, S3 in `Infrastructure/S3/`.
- **Database seeders**: Implement `DA/Seeding/IDataSeeder` interface with `Order` property and `SeedAsync()` method. Seeders are auto-registered by assembly scan.

### Conventions & patterns to follow

- **Feature naming**: Class-per-feature; class name is typically the route name (e.g., `GetExample`); use `nameof(...)` when mapping routes.
- **Validators**: Nested type convention — a nested type named `RequestValidator` is registered automatically by `Utility/Helpers/ServiceCollectionExtensions/ValidatorExtensions.cs` when it implements `IValidator<T>`.
- **DI lifetime**: Features are Transient (assembly-scan in `RegisterFeatures.cs`). UnitOfWork is Scoped. Redis `IConnectionMultiplexer` is Singleton.
- **Claims types**: Use the **standalone** `Utility/Helpers/Common/Auth/KAuthClaimTypes.cs` class. Do NOT create nested duplicates.
- **Email**: Uses MailKit (`Utility/EmailSender/EmailService.cs`) with StartTLS. Constructor takes `senderEmail`, `senderPassword`, `smtpHost`, `smtpPort`.
- **Slack logging**: `Utility/Logger/SlackExceptionLogger.cs` uses `IHttpClientFactory` with resilience (retry + circuit breaker) and `async Task` (never `async void`).
- **AES interface**: `ICustomAESEncryption` — `Encrypt(string) → string`, `Decrypt(string) → string` (Base64 encoded).
- **Specification pattern**: For complex queries, extend `DA/Specifications/BaseSpecification<T>` with criteria, includes, ordering, and paging. Use `GetBySpecAsync`, `ListBySpecAsync`, `CountBySpecAsync` on the repository.
- **API versioning**: Routes are versioned as `/v{version:apiVersion}/FeatureName/Action`. Default version is 1.0. Clients can also pass `X-Api-Version` header or `?api-version=` query string.

### Build / run / test (PowerShell)

```powershell
# Build solution
dotnet build .\projectname.slnx

# Run (env vars come from launchSettings.json)
dotnet run --project .\projectname.Host\ --launch-profile Development

# Run tests
dotnet test .\projectname.slnx

# EF Core migrations
dotnet ef migrations add YourMigration --project DA --startup-project projectname.Host
dotnet ef database update --project DA --startup-project projectname.Host

# Start local dependencies (PostgreSQL, Redis, Seq)
docker compose up -d
```

### Testing

- **Test project**: `projectname.Tests/` — xUnit with **Testcontainers** (real PostgreSQL + Redis containers)
- **Test factory**: `Common/TestWebAppFactory.cs` — implements `IAsyncLifetime`, spins up real containers, overrides DI registrations
- **Test auth**: `Common/TestAuthHelper.cs` — generates valid JWTs matching the test factory's env vars
- **Health tests**: `Features/HealthCheckTests.cs` — tests `/health/live` and `/health/ready`
- **Feature tests**: `Features/ExampleFeatureTests.cs` — tests authenticated and unauthenticated access to `/v1/Example/GetExample`
- **Assertions**: Uses **FluentAssertions** (`.Should().Be(...)` syntax)
- **CI**: GitHub Actions CI + CD workflows in `.github/workflows/`
- Add new tests by creating classes with `IClassFixture<TestWebAppFactory>`

### Database & migrations

- EF Core with `AppDbContext` in `DA/Persistence` with **`EnableRetryOnFailure(5)`** for transient PostgreSQL errors.
- `ConfigureApp.EnsureDatabaseCreated()` runs `MigrateAsync()` automatically on startup with try-catch and `LogCritical` on failure (fail-fast).
- `DbSeeder.SeedAsync()` runs after migrations when **both** `IsDevelopment()` and `SEED_DATABASE=true`. Uses the `IDataSeeder` interface with ordered execution.
- Migrations target the DA project with the Host as startup project.

### Local development with Docker Compose

- `docker-compose.yml` provides PostgreSQL 17, Redis 7, Seq, **Mailpit** (SMTP testing), and **OTEL Collector**
- `docker-compose.override.yml` optionally runs the API itself via `docker compose --profile app up`
- `otel-collector.yaml` at root configures OTLP receivers with debug exporter
- Mailpit Web UI at `http://localhost:8025`, SMTP on port 1025
- All services include health checks and named volumes for data persistence

### Integration points & external dependencies

- **Redis session manager**: `Infrastructure/Redis/RedisSessionManager.cs` — registered as Scoped via `AddRedisSessionManager()`. **Graceful degradation**: all operations wrapped in try-catch with `LogWarning` on `RedisException`.
- **S3**: `Infrastructure/S3/S3Helper.cs` — singleton with AWS credentials.
- **Email (MailKit)**: `Utility/EmailSender/EmailService.cs` — SMTP with StartTLS. Points to Mailpit (port 1025) in development.
- **Slack**: `Utility/Logger/SlackExceptionLogger.cs` — posts exceptions to webhook URL via `IHttpClientFactory` with resilience (retry + circuit breaker).
- **gRPC**: Endpoint exposure via `Utility/EndpointExposerGRPC/`.
- **OpenTelemetry**: Metrics (ASP.NET Core, HttpClient, Runtime, Npgsql) + Tracing (ASP.NET Core, HttpClient, EF Core, Redis) with `RecordException = true`. OTLP exporter enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Service name/version from `OTEL_SERVICE_NAME`/`OTEL_SERVICE_VERSION` env vars. Health endpoints filtered from traces.
- **Serilog + Seq**: Structured JSON logging via `CompactJsonFormatter`. Enriched with `CorrelationId`, `RequestPath`, `RequestMethod`, `MachineName`, `EnvironmentName`, `Service`, `Version`. Seq integration when `SEQ_URL` is set.
- **Health checks**: `/health/live` (liveness), `/health/ready` (readiness — PostgreSQL + Redis + EF Core), `/health` (all checks, **requires authentication**). All return structured JSON with status, duration, and per-check details.
- **API versioning**: `Asp.Versioning.Http` configured with URL segment + `X-Api-Version` header + `?api-version=` query string readers. Default version 1.0. Routes are `/v{version}/FeatureName/Action`.
- **Response compression**: Brotli (`CompressionLevel.Optimal`) + Gzip (`CompressionLevel.SmallestSize`) enabled for HTTPS. Includes `application/json` and `application/grpc` MIME types.
- **HTTP resilience**: `Microsoft.Extensions.Http.Resilience` provides retry (3 attempts), circuit breaker, and timeout policies. Use `AddResilientHttpClient()` extension for new HTTP clients.
- **System.Text.Json**: Global `ConfigureHttpJsonOptions` with `CamelCase` naming, `WhenWritingNull` ignore, `JsonStringEnumConverter`.

### Common gotchas for automated edits

- **Never add hardcoded credentials.** Every secret must come from an env var with `ArgumentException.ThrowIfNullOrWhiteSpace()`. No `??` fallbacks for secrets.
- **Middleware ordering is critical.** See the pipeline order above. MapEndpoints must be last.
- **UserPayload properties are nullable** (`string?`). Always check `IsValid()` or null-check before accessing claims.
- **AES encryption interface changed**: `Decrypt` and `Encrypt` both work with `string` (Base64-encoded). The old `byte[]` interface is gone.
- **GenericRepository was deleted.** Use only `DA/Persistence/Repository/` for data access.
- **NATS was removed.** The `Utility/NATSNotificationSystem/` directory no longer exists.
- **SessionManager moved** from `Utility/SessionManager/` to `Infrastructure/Redis/`.
- **S3Helper moved** from `Utility/Helpers/S3Helper.cs` to `Infrastructure/S3/S3Helper.cs`.
- Follow the nested-validator `RequestValidator` naming convention if adding FluentValidation validators; otherwise they won't be auto-registered.
- **No Console.WriteLine.** Use `Serilog.Log.Information()` for startup/DI logging, or injected `ILogger<T>` for runtime logging.
- **No Newtonsoft.Json in Utility.** Use `System.Text.Json` for serialization. Newtonsoft remains only in Infrastructure (transitive dependency).
- **Central package management**: All NuGet versions are in `Directory.Packages.props`. Do NOT add `Version=` attributes in `.csproj` files.
- **Correlation IDs**: All requests get an `X-Correlation-ID` header (read from request or generated). It flows into Serilog LogContext automatically.
- **EF Core retry**: `EnableRetryOnFailure(5)` is configured — do NOT add manual retry logic around DB calls.
- **Redis operations are resilient**: `RedisSessionManager` catches `RedisException` and returns defaults. Do NOT let Redis failures crash requests.
- **API routes are versioned**: Routes are `/v{version}/FeatureName/Action` (e.g., `/v1/Example/GetExample`). Health endpoints are NOT versioned (`/health/*`).
- **Helm chart secrets are placeholders only.** Never commit actual values. Use `--set` at deploy time or external-secrets in production.
- **SEED_DATABASE env var**: Set to `true` in Development to run seeders. Always `false` in production. Seeders implement `IDataSeeder` and are auto-registered.
- **Specification pattern**: Use `BaseSpecification<T>` for complex queries instead of adding more methods to Repository.cs.

### Files referenced (start here)

- `projectname.Host/Program.cs` — Entry point, env var validation, health checks
- `projectname.Host/Extensions/Resources.cs` — DI composition root
- `projectname.Host/Extensions/ConfigureApp.cs` — Middleware pipeline
- `projectname.Host/Extensions/Validators/Validator.cs` — JWT validation config
- `projectname.Host/Extensions/OpenTelemetryExtensions.cs` — OTEL metrics + tracing
- `projectname.Host/Extensions/ApiVersioningExtensions.cs` — API version config
- `projectname.Host/Extensions/ResilienceExtensions.cs` — HTTP resilience helper
- `projectname.Host/Middlewares/CorrelationIdMiddleware.cs` — Request correlation
- `projectname.Host/Properties/launchSettings.json` — All environment variables
- `Core/Endpoints/RegisterFeatures.cs` — Feature auto-registration
- `Core/Features/Example/ExampleFeatures.cs` — Example endpoint
- `Utility/EndpointController/IFeature.cs` — Feature interface
- `DA/Persistence/UnitOfWork.cs` — Unit of work pattern
- `DA/Persistence/DbSeeder.cs` — Development seed data (IDataSeeder-based)
- `DA/Seeding/IDataSeeder.cs` — Seeder interface
- `DA/Specifications/BaseSpecification.cs` — Specification pattern base
- `DA/Specifications/SpecificationEvaluator.cs` — Specification evaluator
- `DA/DependencyInjection.cs` — DB context registration (EnableRetryOnFailure)
- `Infrastructure/Redis/RedisSessionManager.cs` — Redis session management (resilient)
- `Infrastructure/S3/S3Helper.cs` — AWS S3 integration
- `projectname.Tests/Common/TestWebAppFactory.cs` — Testcontainers-based test factory
- `projectname.Tests/Common/TestAuthHelper.cs` — Test JWT generator
- `.github/workflows/ci.yml` — CI pipeline (build, test, code quality)
- `.github/workflows/cd.yml` — CD pipeline (Docker build + push to GHCR)
- `docker-compose.yml` — Local dev (PostgreSQL, Redis, Seq, Mailpit, OTEL Collector)
- `otel-collector.yaml` — OTEL Collector configuration
- `helm/projectname/` — Kubernetes Helm chart (deployment, service, ingress, HPA, secrets)

