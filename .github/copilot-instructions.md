## Quick orientation for AI code agents

This repository is an ASP.NET minimal API **template** (features-as-classes pattern) with a small suite of utilities (logging, email, NATS, Redis). The goal of this document is to give an AI agent the minimal, concrete facts needed to be productive here.

**Note:** This is a template project. Replace `__ProjectName__` with your actual project name when setting up a new project.

Key facts (first things to know)
- Entry point: `__ProjectName__.Host/Program.cs` — it calls `builder.Services.RegisterService(builder.Configuration)` and `await app.Configure()`.
- Service registration: `__ProjectName__.Host/Extensions/Resources.cs` (extension `RegisterService`) wires core services (logging, Redis session manager, auth, business layer, validators, NATS, email).
- Endpoint pattern: endpoints are classes implementing `Utility/EndpointController/IFeature.cs` (single method: `void Map(IEndpointRouteBuilder app)`). All feature classes are registered by assembly scan in `Core/Endpoints/RegisterFeatures.cs` and then mapped in `__ProjectName__.Host/Extensions/ConfigureApp.cs` with `app.MapEndpoints()`.

Where to add code
- New HTTP endpoints: create a class under `Core/Features/...` implementing `IFeature` and implement `Map(...)`. Example: `Core/Features/Example/ExampleFeatures.cs` demonstrates the pattern.
- Dependency injection / services: add registrations in a relevant `DependencyInjection.cs` (examples: `DA/DependencyInjection.cs`, `Core/DependencyInjection.cs`, `Utility/*/DependencyInjection.cs`). The host composition is done in `RegisterService` (see `Resources.cs`).
- Data access: repository & unit-of-work live under `DA/Persistence` (`UnitOfWork.cs` and `Repository` pattern). Use `IUnitOfWork` in endpoint handlers by constructor or method injection.

Conventions & patterns to follow
- Feature naming: class-per-feature; class name is typically the route name (e.g., `GetExample`); use `nameof(...)` when mapping routes to avoid hardcoded strings.
- Validators: optional nested type convention — a nested type named `RequestValidator` is registered automatically by `Utility/Helpers/ServiceCollectionExtensions/ValidatorExtensions.cs` when it implements `IValidator<T>`. Place nested validators inside the corresponding feature class.
- DI lifetime: most features are registered Transient (assembly-scan in `RegisterFeatures.cs`). UnitOfWork is registered Scoped via `DA/DependencyInjection.cs`.
- Environment variables: the app reads config from env vars with sensible defaults in code. Notable keys: `DBHost` (default: local Postgres connection string fallback), `RedisHost`, `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`. Keep these in mind when running locally.

Build / run / debug (PowerShell)
- Build solution: `$PS> dotnet build .\__ProjectName__.slnx`
- Run the host project:
  - Set required env vars for the session, e.g.:
    `$PS> $env:DBHost = "Host=localhost;Port=5432;Database=__ProjectName__Db;Username=postgres;Password=postgres"`
    `$PS> $env:RedisHost = "localhost:6379"`
  - Run:
    `$PS> dotnet run --project .\__ProjectName__.Host\ --configuration Debug`
- To run from IDE (Rider/Visual Studio) open `__ProjectName__.slnx` and run `__ProjectName__.Host`.

Database & migrations
- The project uses EF Core with `AppDbContext` in `DA/Persistence`. The host attempts to ensure DB in `ConfigureApp.EnsureDatabaseCreated()` and applies pending migrations automatically. Migrations commands should be run against the DA project:
  - `dotnet ef migrations add YourMigration --project DA --startup-project __ProjectName__.Host`
  - `dotnet ef database update --project DA --startup-project __ProjectName__.Host`

Integration points & external dependencies
- Redis session manager: `__ProjectName__.Host/Extensions/Resources.cs` registers a `IConnectionMultiplexer` using `RedisHost`.
- NATS JetStream: utility code exists under `Utility/NATSNotificationSystem/*`. DI wiring is present but some lines are commented — check `AddNatsService` before enabling in production.
- Email: `Utility/EmailSender` is wired via `RegisterService`.

Quick examples (where to look)
- How endpoints are registered: `Core/Endpoints/RegisterFeatures.cs` and `__ProjectName__.Host/Extensions/ConfigureApp.cs`.
- Example feature implementation: `Core/Features/Example/ExampleFeatures.cs` (demonstrates GET and POST with validation).
- Unit-of-work/repository: `DA/Persistence/UnitOfWork.cs`.
- DI composition root: `__ProjectName__.Host/Extensions/Resources.cs` and `Core/DependencyInjection.cs`.

Common gotchas for automated edits
- The project relies on environment variables; tests or local runs may fail silently if DB/Redis env vars are missing. When modifying DB schema, also update migrations to avoid runtime errors.
- Be conservative when enabling commented code (e.g., NATS wiring) — those comments often indicate unfinished or environment-specific wiring.
- Follow the nested-validator `RequestValidator` naming convention if adding FluentValidation validators; otherwise they won't be auto-registered.

Files referenced (start here):
- `__ProjectName__.Host/Program.cs`
- `__ProjectName__.Host/Extensions/Resources.cs`
- `__ProjectName__.Host/Extensions/ConfigureApp.cs`
- `Core/Endpoints/RegisterFeatures.cs`
- `Utility/EndpointController/IFeature.cs`
- `DA/Persistence/UnitOfWork.cs`
- `DA/DependencyInjection.cs`
- `Core/Features/Example/ExampleFeatures.cs`
