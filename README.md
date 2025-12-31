# Project Setup & Overview

Welcome to your new .NET 10 minimal API service project!

## Rename the Project

Before starting, rename the template to your desired project name using the provided PowerShell script:

```powershell
./setup-template.ps1 -ProjectName <YourProjectName>
```

This will update all placeholders and file names to match your chosen project name.

## Getting Started

1. **Install Prerequisites**
   - [.NET 10 SDK](https://dotnet.microsoft.com/download)
   - [PostgreSQL](https://www.postgresql.org/download/) (for local DB)
   - [Redis](https://redis.io/download) (for session management)

2. **Configure Environment Variables**
   - Set required environment variables before running:
     ```powershell
     $env:DBHost = "Host=localhost;Port=5432;Database=<ProjectName>Db;Username=postgres;Password=postgres"
     $env:RedisHost = "localhost:6379"
     $env:JWT_KEY = "your_jwt_key"
     $env:JWT_ISSUER = "your_issuer"
     $env:JWT_AUDIENCE = "your_audience"
     ```

3. **Build & Run**
   - Build the solution:
     ```powershell
     dotnet build .\<ProjectName>.slnx
     ```
   - Run the host project:
     ```powershell
     dotnet run --project .\<ProjectName>.Host\ --configuration Debug
     ```

4. **Database Migrations**
   - Create initial migration:
     ```powershell
     dotnet ef migrations add Init --project DA --startup-project <ProjectName>.Host
     ```
   - Apply migrations:
     ```powershell
     dotnet ef database update --project DA --startup-project <ProjectName>.Host
     ```

## Project Structure

- `<ProjectName>.Host/` — Entry point, DI composition, app configuration
  - `Program.cs` — Main startup file
  - `Extensions/Resources.cs` — Registers core services (logging, Redis, auth, business layer, validators, NATS, email)
  - `Extensions/ConfigureApp.cs` — Maps endpoints, ensures DB is created
- `Core/` — Business logic, endpoint features
  - `Features/` — Each feature (endpoint) as a class implementing `IFeature`
  - `Endpoints/RegisterFeatures.cs` — Scans and registers all features
  - `DependencyInjection.cs` — Registers core services
- `DA/` — Data access layer
  - `Persistence/UnitOfWork.cs` — Unit-of-work pattern
  - `DependencyInjection.cs` — Registers data access services
- `Utility/` — Utilities (logging, email, NATS, Redis, endpoint controller)
  - `EndpointController/IFeature.cs` — Feature endpoint interface
  - `Helpers/ServiceCollectionExtensions/ValidatorExtensions.cs` — Auto-registers validators

## Conventions

- **Endpoints:** Implemented as classes in `Core/Features`, registered via assembly scan
- **Dependency Injection:** Services registered in respective `DependencyInjection.cs` files
- **Validators:** Use nested `RequestValidator` types for auto-registration
- **Environment Variables:** Used for DB, Redis, JWT config

## Example Endpoints
See `Core/Features/Example/ExampleFeatures.cs` for GET/POST endpoint patterns and validation.

## Next Steps
- Update environment variables in `Properties/launchSettings.json` as needed
- Add new features by creating classes in `Core/Features/`
- Review and customize DI registrations in `Resources.cs` and `DependencyInjection.cs`

---

For more details, see the inline documentation in each file and the `.github/copilot-instructions.md` for AI agent guidance.
