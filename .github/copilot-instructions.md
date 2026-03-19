## Quick orientation for AI code agents

This repository is an ASP.NET minimal API **template** (features-as-classes pattern) targeting .NET 10 with PostgreSQL, Redis, and gRPC. The goal of this document is to give an AI agent the minimal, concrete facts needed to be productive here.

**Note:** This is a template project. Replace `__ProjectName__` with your actual project name when setting up a new project via `setup-template.ps1`.

---

### Solution structure (6 projects)

```
__ProjectName__.Host   ← Entry point, middleware pipeline, DI composition root
Core                   ← Features (endpoints), business layer DI
DA                     ← Data access (EF Core, Repository, UnitOfWork)
Utility                ← Shared helpers (Auth/JWT/AES, Email, Logging, gRPC, Validators)
Infrastructure         ← External service integrations (Redis session management, S3)
Auth                   ← Auth project scaffold (JWT, TOTP, AES — migration in progress)
```

### Key facts (first things to know)

- Entry point: `__ProjectName__.Host/Program.cs` — calls `ValidateRequiredEnvironmentVariables()`, then `builder.Services.RegisterService(builder.Configuration)` and `await app.Configure()`.
- Service registration: `__ProjectName__.Host/Extensions/Resources.cs` (extension `RegisterService`) wires all services: Redis, Auth/JWT, CORS, Rate Limiting, Email, gRPC, OpenAPI.
- Endpoint pattern: endpoints are classes implementing `Utility/EndpointController/IFeature.cs` (single method: `void Map(IEndpointRouteBuilder app)`). All feature classes are registered by assembly scan in `Core/Endpoints/RegisterFeatures.cs` and then mapped in `__ProjectName__.Host/Extensions/ConfigureApp.cs` with `app.MapEndpoints()`.

### Middleware pipeline order (ConfigureApp.cs)

The pipeline is **order-sensitive**. The correct order is:

```
1. UseCors()                                    — preflight OPTIONS handling
2. Security Headers middleware (inline)          — X-Content-Type-Options, X-Frame-Options, HSTS, etc.
3. UseMiddleware<GlobalExceptionHandlerMiddleware>  — catches all errors
4. UseHttpsRedirection()                         — HTTP → HTTPS redirect
5. UseAuthentication()                           — JWT validation
6. UseAuthorization()                            — policy evaluation
7. UseMiddleware<UserContextMiddleware>           — populates IUserContext from claims
8. UseRateLimiter()                              — 100 req/min per IP (fixed window)
9. GrpcServices()                                — gRPC service mapping
10. MapEndpoints()                               — LAST — terminates the pipeline
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

Additional optional env vars: `RedisHost`, `SMTP_HOST`, `SMTP_PORT`, `SCALAR_BEARER_TOKEN`, `SLACK_WEBHOOK_URL`, `SERVER_URL`, `SEQ_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `AWS_*`, `S3_BUCKET_NAME`.

All env vars are defined in `__ProjectName__.Host/Properties/launchSettings.json`. **No hardcoded credential fallbacks are allowed** — every secret read must use `ArgumentException.ThrowIfNullOrWhiteSpace()`.

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
- **Data access**: Repository & unit-of-work live under `DA/Persistence` (`UnitOfWork.cs` and `Repository` pattern). Use `IUnitOfWork` in endpoint handlers.
- **Infrastructure concerns**: Redis session management in `Infrastructure/Redis/`, S3 in `Infrastructure/S3/`.

### Conventions & patterns to follow

- **Feature naming**: Class-per-feature; class name is typically the route name (e.g., `GetExample`); use `nameof(...)` when mapping routes.
- **Validators**: Nested type convention — a nested type named `RequestValidator` is registered automatically by `Utility/Helpers/ServiceCollectionExtensions/ValidatorExtensions.cs` when it implements `IValidator<T>`.
- **DI lifetime**: Features are Transient (assembly-scan in `RegisterFeatures.cs`). UnitOfWork is Scoped. Redis `IConnectionMultiplexer` is Singleton.
- **Claims types**: Use the **standalone** `Utility/Helpers/Common/Auth/KAuthClaimTypes.cs` class. Do NOT create nested duplicates.
- **Email**: Uses MailKit (`Utility/EmailSender/EmailService.cs`) with StartTLS. Constructor takes `senderEmail`, `senderPassword`, `smtpHost`, `smtpPort`.
- **Slack logging**: `Utility/Logger/SlackExceptionLogger.cs` uses `IHttpClientFactory` and `async Task` (never `async void`).
- **AES interface**: `ICustomAESEncryption` — `Encrypt(string) → string`, `Decrypt(string) → string` (Base64 encoded).

### Build / run / debug (PowerShell)

```powershell
# Build solution
dotnet build .\__ProjectName__.slnx

# Run (env vars come from launchSettings.json)
dotnet run --project .\__ProjectName__.Host\ --launch-profile Development

# EF Core migrations
dotnet ef migrations add YourMigration --project DA --startup-project __ProjectName__.Host
dotnet ef database update --project DA --startup-project __ProjectName__.Host
```

### Database & migrations

- EF Core with `AppDbContext` in `DA/Persistence`. 
- `ConfigureApp.EnsureDatabaseCreated()` runs `MigrateAsync()` automatically on startup.
- Migrations target the DA project with the Host as startup project.

### Integration points & external dependencies

- **Redis session manager**: `Infrastructure/Redis/RedisSessionManager.cs` — registered as Scoped via `AddRedisSessionManager()`.
- **S3**: `Infrastructure/S3/S3Helper.cs` — singleton with AWS credentials.
- **Email (MailKit)**: `Utility/EmailSender/EmailService.cs` — SMTP with StartTLS.
- **Slack**: `Utility/Logger/SlackExceptionLogger.cs` — posts exceptions to webhook URL via `IHttpClientFactory`.
- **gRPC**: Endpoint exposure via `Utility/EndpointExposerGRPC/`.
- **OpenTelemetry**: Optional, enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- **Serilog + Seq**: Serilog for structured logging; Seq integration when `SEQ_URL` is set.

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

### Files referenced (start here)

- `__ProjectName__.Host/Program.cs` — Entry point, env var validation
- `__ProjectName__.Host/Extensions/Resources.cs` — DI composition root
- `__ProjectName__.Host/Extensions/ConfigureApp.cs` — Middleware pipeline
- `__ProjectName__.Host/Extensions/Validators/Validator.cs` — JWT validation config
- `__ProjectName__.Host/Properties/launchSettings.json` — All environment variables
- `Core/Endpoints/RegisterFeatures.cs` — Feature auto-registration
- `Core/Features/Example/ExampleFeatures.cs` — Example endpoint
- `Utility/EndpointController/IFeature.cs` — Feature interface
- `DA/Persistence/UnitOfWork.cs` — Unit of work pattern
- `DA/DependencyInjection.cs` — DB context registration
- `Infrastructure/Redis/RedisSessionManager.cs` — Redis session management
- `Infrastructure/S3/S3Helper.cs` — AWS S3 integration
