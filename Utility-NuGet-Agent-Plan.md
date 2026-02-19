# Agent Plan: Extract Tahaq/Utility into NuGet Packages

> Step-by-step instructions for an AI agent to convert `Tahaq/Utility/` into individual NuGet packages.
> Source: `d:\dotnet\QA-Orient\Tahaq\Utility\`

---

## Prerequisites

- .NET 10 SDK installed
- Access to a NuGet feed (GitHub Packages, Azure Artifacts, or local folder)
- The agent must read every source file referenced below before modifying it

---

## Phase 1: Create the Package Repository Structure

### Step 1.1 — Create the solution

Create a new directory `tahaq-packages/` at the same level as `Tahaq/`. Initialize a solution with the following structure:

```
tahaq-packages/
├── tahaq-packages.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── src/
│   ├── Tahaq.Core/
│   ├── Tahaq.Data/
│   ├── Tahaq.Auth/
│   ├── Tahaq.Redis/
│   ├── Tahaq.Logging/
│   ├── Tahaq.Email/
│   ├── Tahaq.Grpc/
│   └── Tahaq.Nats/
└── README.md
```

### Step 1.2 — Create `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>1.0.0</Version>
    <Authors>QA-Orient Team</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>
</Project>
```

> Target `net8.0` so all current services (Oaken on .NET 10, QI on .NET 8) can consume the packages. .NET 10 projects can reference .NET 8 packages without issues.

### Step 1.3 — Create `Directory.Packages.props`

Copy version entries from `d:\dotnet\QA-Orient\Tahaq\Directory.Packages.props` but only include packages needed by the Utility code. Exclude Host-only packages (Scalar, OpenTelemetry, SkiaSharp, EPPlus, Mediator, Containers.Tools).

```xml
<Project>
  <ItemGroup>
    <!-- EF Core -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.8" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />

    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="8.0.2" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="8.0.1" />

    <!-- Validation -->
    <PackageVersion Include="FluentValidation" Version="11.9.2" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />

    <!-- Logging -->
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />

    <!-- Serialization -->
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

    <!-- Redis -->
    <PackageVersion Include="StackExchange.Redis" Version="2.8.16" />

    <!-- gRPC -->
    <PackageVersion Include="Grpc.AspNetCore" Version="2.66.0" />
    <PackageVersion Include="Grpc.Tools" Version="2.66.0" />
    <PackageVersion Include="Google.Protobuf" Version="3.28.0" />

    <!-- NATS -->
    <PackageVersion Include="NATS.Client.JetStream" Version="2.3.3" />

    <!-- Auth -->
    <PackageVersion Include="OtpSharp.Core" Version="1.0.0" />
  </ItemGroup>
</Project>
```

---

## Phase 2: Create Each Package

### Package 1: Tahaq.Core

#### Step 2.1.1 — Create project file

Create `src/Tahaq.Core/Tahaq.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Core abstractions, response models, endpoint filters, and utilities for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

#### Step 2.1.2 — Copy and refactor source files

Copy these files from `Tahaq/Utility/` into `src/Tahaq.Core/`, updating namespaces from `Utility.*` to `Tahaq.Core.*`:

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `EndpointController/IFeature.cs` | `Endpoints/IFeature.cs` | `Tahaq.Core.Endpoints` |
| `Helpers/Common/ApiResponseModel.cs` | `Response/ApiResponseModel.cs` | `Tahaq.Core.Response` |
| `Helpers/Common/Filters/RequestValidationFilter.cs` | `Filters/RequestValidationFilter.cs` | `Tahaq.Core.Filters` |
| `Helpers/Common/Filters/RequestLoggingFilter.cs` | `Filters/RequestLoggingFilter.cs` | `Tahaq.Core.Filters` |
| `Helpers/RouteHandler/RouteHandlerBuilderValidationExtensions.cs` | `Extensions/RouteHandlerBuilderValidationExtensions.cs` | `Tahaq.Core.Extensions` |
| `Helpers/ServiceCollectionExtensions/ValidatorExtensions.cs` | `Extensions/ValidatorExtensions.cs` | `Tahaq.Core.Extensions` |
| `Helpers/CustomExceptionThrower/ArgumentFalseException.cs` | `Exceptions/ArgumentFalseException.cs` | `Tahaq.Core.Exceptions` |
| `Helpers/CustomExceptionThrower/ArgumentThrowCustom.cs` | `Exceptions/ArgumentThrowCustom.cs` | `Tahaq.Core.Exceptions` |
| `Helpers/StringsExtension/StringTransformer.cs` | `Utilities/StringTransformer.cs` | `Tahaq.Core.Utilities` |
| `Helpers/StringsExtension/NanoId.cs` | `Utilities/NanoId.cs` | `Tahaq.Core.Utilities` |
| `Helpers/StringsExtension/PasswordHelper.cs` | `Utilities/PasswordHelper.cs` | `Tahaq.Core.Utilities` |
| `Helpers/AsyncTimeoutHelper.cs` | `Utilities/AsyncTimeoutHelper.cs` | `Tahaq.Core.Utilities` |
| `Helpers/ConfigurationExtension.cs` | `Extensions/ConfigurationExtension.cs` | `Tahaq.Core.Extensions` |
| `CustomHTTP/HTTPCommonCodes.cs` | `Http/HTTPCommonCodes.cs` | `Tahaq.Core.Http` |
| `Helpers/CommonModels/ActivityTrackersInResponse.cs` | `Models/ActivityTrackersInResponse.cs` | `Tahaq.Core.Models` |

#### Step 2.1.3 — Modify `IFeature.cs`

Change from instance method to static abstract (requires .NET 7+):

```csharp
namespace Tahaq.Core.Endpoints;

public interface IFeature
{
    static abstract void Map(IEndpointRouteBuilder app);
}
```

#### Step 2.1.4 — Remove hardcoded values

**Do NOT copy** `KConstant.cs`, `KDefinedRoles.cs`, or `AppConstants` into the package. These are domain-specific.

Instead, create an options class:

```csharp
// src/Tahaq.Core/Configuration/TahaqCoreOptions.cs
namespace Tahaq.Core.Configuration;

public class TahaqCoreOptions
{
    public string ApiName { get; set; } = "Api";
}
```

Add an extension method:

```csharp
// src/Tahaq.Core/Extensions/ServiceCollectionExtensions.cs
namespace Tahaq.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTahaqCore(this IServiceCollection services, Action<TahaqCoreOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<TahaqCoreOptions>(_ => { });

        return services;
    }
}
```

#### Step 2.1.5 — Clean up

- Remove all `Console.WriteLine` calls
- Remove all commented-out code
- Rename `Genertor` class in `NanoId.cs` to `NanoIdGenerator` (fix typo)

---

### Package 2: Tahaq.Data

#### Step 2.2.1 — Create project file

Create `src/Tahaq.Data/Tahaq.Data.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Generic repository and Unit of Work abstractions for EF Core with PostgreSQL.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
</Project>
```

#### Step 2.2.2 — Copy and refactor source files

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `GenericRepository/IGenericRepository.cs` | `Repository/IGenericRepository.cs` | `Tahaq.Data.Repository` |
| `GenericRepository/GenericRepository.cs` | `Repository/GenericRepository.cs` | `Tahaq.Data.Repository` |
| `GenericRepository/CommonModels/Base.cs` | `Models/Base.cs` | `Tahaq.Data.Models` |
| `GenericRepository/CommonModels/GetterResult.cs` | `Models/GetterResult.cs` | `Tahaq.Data.Models` |
| `GenericRepository/CommonModels/SetterResult.cs` | `Models/SetterResult.cs` | `Tahaq.Data.Models` |
| `GenericRepository/CommonModels/PaginationParameters.cs` | `Models/PaginationParameters.cs` | `Tahaq.Data.Models` |
| `GenericRepository/CommonModels/CommonMessages.cs` | `Models/CommonMessages.cs` | `Tahaq.Data.Models` |
| `GenericRepository/PosgtreSqlStates.cs` | `PostgreSql/PostgreSqlStates.cs` | `Tahaq.Data.PostgreSql` |

#### Step 2.2.3 — Critical fix: sanitize exception messages

In `GenericRepository.cs`, every catch block returns `ex.Message` to the caller. Replace with generic messages and log the actual exception:

```csharp
// BEFORE (current):
catch (Exception ex)
{
    return (false, ex.Message);
}

// AFTER:
catch (Exception ex)
{
    // Log ex internally if ILogger is available
    return (false, "An unexpected error occurred.");
}
```

Consider adding an optional `ILogger<GenericRepository<TEntity, PrimitiveType>>` parameter to the constructor.

#### Step 2.2.4 — Fix typo

Rename `PosgtreSqlStates.cs` to `PostgreSqlStates.cs`.

#### Step 2.2.5 — Remove commented-out code

Remove all commented-out method blocks (lines ~205-208, ~482-515 in the current file).

---

### Package 3: Tahaq.Auth

#### Step 2.3.1 — Create project file

Create `src/Tahaq.Auth/Tahaq.Auth.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>JWT authentication, custom authorization, TOTP, and AES encryption for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="OtpSharp.Core" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

#### Step 2.3.2 — Copy and refactor source files

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `Helpers/Auth/JWT/JWT.cs` | `Jwt/JwtService.cs` | `Tahaq.Auth.Jwt` |
| `Helpers/Auth/Models/UserPayload.cs` | `Models/UserPayload.cs` | `Tahaq.Auth.Models` |
| `Helpers/Auth/HTTPContextUserRetriever.cs` | `Context/HttpContextUserRetriever.cs` | `Tahaq.Auth.Context` |
| `Helpers/Auth/Middlewares/UserContext.cs` | `Middleware/UserContextMiddleware.cs` | `Tahaq.Auth.Middleware` |
| `Helpers/Common/Auth/CustomAuthorizationHandler.cs` | `Authorization/CustomAuthorizationHandler.cs` | `Tahaq.Auth.Authorization` |
| `Helpers/Common/Auth/KPolicyDescriptor.cs` | `Authorization/PolicyDescriptor.cs` | `Tahaq.Auth.Authorization` |
| `Helpers/Common/Auth/KAuthClaimTypes.cs` | `Claims/AuthClaimTypes.cs` | `Tahaq.Auth.Claims` |
| `Helpers/Common/Auth/Requirements/CustomAuthorizationRequirement.cs` | `Authorization/CustomAuthorizationRequirement.cs` | `Tahaq.Auth.Authorization` |
| `AuthProvider/TOTPProvider.cs` | `Totp/TotpProvider.cs` | `Tahaq.Auth.Totp` |
| `AuthProvider/Base32Encoder.cs` | `Totp/Base32Encoder.cs` | `Tahaq.Auth.Totp` |
| `AuthProvider/AESEncryption/ICustomAESEncryption.cs` | `Encryption/IAesEncryption.cs` | `Tahaq.Auth.Encryption` |
| `AuthProvider/AESEncryption/CustomAESEncryption.cs` | `Encryption/AesEncryption.cs` | `Tahaq.Auth.Encryption` |

#### Step 2.3.3 — Create options class and DI extension

Replace direct environment variable reads with an options pattern:

```csharp
// src/Tahaq.Auth/Configuration/TahaqAuthOptions.cs
namespace Tahaq.Auth.Configuration;

public class TahaqAuthOptions
{
    public string JwtKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string? TotpKey { get; set; }
    public string? TotpIv { get; set; }
}
```

```csharp
// src/Tahaq.Auth/Extensions/ServiceCollectionExtensions.cs
namespace Tahaq.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTahaqAuth(
        this IServiceCollection services,
        Action<TahaqAuthOptions> configure)
    {
        var options = new TahaqAuthOptions();
        configure(options);

        ArgumentException.ThrowIfNullOrEmpty(options.JwtKey, nameof(options.JwtKey));
        ArgumentException.ThrowIfNullOrEmpty(options.Issuer, nameof(options.Issuer));
        ArgumentException.ThrowIfNullOrEmpty(options.Audience, nameof(options.Audience));

        services.Configure(configure);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,       // FIX: was false with TODO
                    ValidateAudience = true,     // FIX: was false with TODO
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(options.JwtKey))
                };
            });

        services.AddAuthorization(authOptions =>
        {
            authOptions.AddPolicy(PolicyDescriptor.CustomPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new CustomAuthorizationRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, CustomAuthorizationHandler>();
        services.AddSingleton<IAesEncryption, AesEncryption>();

        return services;
    }
}
```

#### Step 2.3.4 — Fix the authorization handler

In `CustomAuthorizationHandler.cs`, replace `//TODO:loging` comments with actual `ILogger` usage:

```csharp
public class CustomAuthorizationHandler : AuthorizationHandler<CustomAuthorizationRequirement>
{
    private readonly ILogger<CustomAuthorizationHandler> _logger;

    public CustomAuthorizationHandler(ILogger<CustomAuthorizationHandler> logger, ...)
    {
        _logger = logger;
    }

    // In failure paths:
    _logger.LogWarning("Authorization failed: {Reason}", reason);
}
```

#### Step 2.3.5 — Remove hardcoded JWT fallback

The current code has:
```csharp
var Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "asdavvasd132132131231232312312dsadasdsdsdsds@asd112";
```

This is eliminated by the options pattern — the `AddTahaqAuth()` method throws if `JwtKey` is null or empty.

---

### Package 4: Tahaq.Redis

#### Step 2.4.1 — Create project file

Create `src/Tahaq.Redis/Tahaq.Redis.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Redis session management for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StackExchange.Redis" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

#### Step 2.4.2 — Copy and refactor

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `SessionManager/RedisSessionManager.cs` | `RedisSessionManager.cs` | `Tahaq.Redis` |

#### Step 2.4.3 — Create options and DI extension

```csharp
// src/Tahaq.Redis/Configuration/TahaqRedisOptions.cs
namespace Tahaq.Redis.Configuration;

public class TahaqRedisOptions
{
    public string ConnectionString { get; set; } = null!;
}
```

```csharp
// src/Tahaq.Redis/Extensions/ServiceCollectionExtensions.cs
namespace Tahaq.Redis.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTahaqRedis(
        this IServiceCollection services,
        Action<TahaqRedisOptions> configure)
    {
        var options = new TahaqRedisOptions();
        configure(options);

        ArgumentException.ThrowIfNullOrEmpty(options.ConnectionString, nameof(options.ConnectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { options.ConnectionString }
            }));

        services.AddScoped<RedisSessionManager>();

        return services;
    }
}
```

---

### Package 5: Tahaq.Logging

#### Step 2.5.1 — Create project file

Create `src/Tahaq.Logging/Tahaq.Logging.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Custom Serilog file logger and Slack exception reporting for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Newtonsoft.Json" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Http" />
  </ItemGroup>
</Project>
```

#### Step 2.5.2 — Copy and refactor

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `Logger/ICustomLogger.cs` | `ICustomLogger.cs` | `Tahaq.Logging` |
| `Logger/LoggerImpl.cs` | `SerilogLogger.cs` | `Tahaq.Logging` |
| `Logger/SlackExceptionMethods.cs` | `Slack/SlackExceptionReporter.cs` | `Tahaq.Logging.Slack` |

#### Step 2.5.3 — Create options and DI extension

```csharp
// src/Tahaq.Logging/Configuration/TahaqLoggingOptions.cs
namespace Tahaq.Logging.Configuration;

public class TahaqLoggingOptions
{
    public string LogFilePath { get; set; } = "logs/app-Logs.txt";
    public string? SlackWebhookUrl { get; set; }
    public string AppName { get; set; } = "App";
}
```

```csharp
// src/Tahaq.Logging/Extensions/ServiceCollectionExtensions.cs
namespace Tahaq.Logging.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTahaqLogging(
        this IServiceCollection services,
        Action<TahaqLoggingOptions> configure)
    {
        var options = new TahaqLoggingOptions();
        configure(options);

        services.Configure(configure);
        services.AddScoped<ICustomLogger>(_ =>
            new SerilogLogger(
                new LoggerConfiguration()
                    .WriteTo.File(options.LogFilePath, rollingInterval: RollingInterval.Day)
                    .CreateLogger()));

        return services;
    }
}
```

#### Step 2.5.4 — Fix SlackExceptionMethods

- Remove hardcoded `AppName = "ageeks"` — read from `TahaqLoggingOptions.AppName`
- Remove static `HttpClient` instantiation per call — use `IHttpClientFactory`
- Remove `async void` — change to `async Task`

---

### Package 6: Tahaq.Email

#### Step 2.6.1 — Create project file

Create `src/Tahaq.Email/Tahaq.Email.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Email sending service for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

> Note: The current `EmailService.cs` in Tahaq/Utility uses `System.Net.Mail`, not MailKit. MailKit is in the Core project. Keep this package dependency-light.

#### Step 2.6.2 — Copy and refactor

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `EmailSender/IEmailService.cs` | `IEmailService.cs` | `Tahaq.Email` |
| `EmailSender/EmailService.cs` | `SmtpEmailService.cs` | `Tahaq.Email` |

#### Step 2.6.3 — Create options and DI extension

```csharp
// src/Tahaq.Email/Configuration/TahaqEmailOptions.cs
namespace Tahaq.Email.Configuration;

public class TahaqEmailOptions
{
    public string SenderEmail { get; set; } = null!;
    public string SenderPassword { get; set; } = null!;
    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; } = 587;
}
```

```csharp
// src/Tahaq.Email/Extensions/ServiceCollectionExtensions.cs
namespace Tahaq.Email.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTahaqEmail(
        this IServiceCollection services,
        Action<TahaqEmailOptions> configure)
    {
        var options = new TahaqEmailOptions();
        configure(options);

        ArgumentException.ThrowIfNullOrEmpty(options.SenderEmail, nameof(options.SenderEmail));
        ArgumentException.ThrowIfNullOrEmpty(options.SmtpHost, nameof(options.SmtpHost));

        services.AddSingleton<IEmailService>(_ =>
            new SmtpEmailService(options.SenderEmail, options.SenderPassword, options.SmtpHost, options.SmtpPort));

        return services;
    }
}
```

---

### Package 7: Tahaq.Grpc

#### Step 2.7.1 — Create project file

Create `src/Tahaq.Grpc/Tahaq.Grpc.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>gRPC endpoint discovery and exposure for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <Protobuf Include="Protos\ActionExposed.proto" GrpcServices="Server" />
  </ItemGroup>
</Project>
```

#### Step 2.7.2 — Copy and refactor

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `EndpointExposerGRPC/Exposed/ExposedAllEndpoints.cs` | `Discovery/ExposedAllEndpoints.cs` | `Tahaq.Grpc.Discovery` |
| `EndpointExposerGRPC/Server/ActionExposedImpl.cs` | `Server/ActionExposedImpl.cs` | `Tahaq.Grpc.Server` |
| `EndpointExposerGRPC/ResourceAndConfigMap/Configure.cs` | `Extensions/ApplicationBuilderExtensions.cs` | `Tahaq.Grpc.Extensions` |
| `EndpointExposerGRPC/Exposed/ActionExposed.proto` | `Protos/ActionExposed.proto` | — |
| `Helpers/Singleton/ReadGRPCEndpoints.cs` | `Client/GrpcEndpointRegistry.cs` | `Tahaq.Grpc.Client` |

#### Step 2.7.3 — Refactor ReadGRPCEndpoints

Replace the manual singleton with a proper DI-registered service:

```csharp
// src/Tahaq.Grpc/Configuration/TahaqGrpcOptions.cs
namespace Tahaq.Grpc.Configuration;

public class GrpcEndpointConfig
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string? Password { get; set; }
}

public class TahaqGrpcOptions
{
    public Dictionary<string, GrpcEndpointConfig> Endpoints { get; set; } = new();
}
```

```csharp
// src/Tahaq.Grpc/Client/GrpcEndpointRegistry.cs
namespace Tahaq.Grpc.Client;

public class GrpcEndpointRegistry
{
    private readonly TahaqGrpcOptions _options;

    public GrpcEndpointRegistry(IOptions<TahaqGrpcOptions> options)
    {
        _options = options.Value;
    }

    public GrpcEndpointConfig Get(string serviceName)
    {
        if (_options.Endpoints.TryGetValue(serviceName, out var config))
            return config;
        throw new KeyNotFoundException($"gRPC endpoint not configured: {serviceName}");
    }
}
```

#### Step 2.7.4 — Remove hardcoded gRPC passwords

The current fallback `"parser,ai-srv.qbscocloud.net,32089,password123|..."` is eliminated by the options pattern.

#### Step 2.7.5 — Remove `Console.WriteLine` calls

Remove `Console.WriteLine($"GRPC endpoints loading in api")` and `Console.WriteLine($"{endpoint} loaded")`.

#### Step 2.7.6 — Do NOT copy pre-compiled proto output

Delete `Protos/Compiled/` directory. Let the proto be compiled at build time from the `.proto` file.

---

### Package 8: Tahaq.Nats

#### Step 2.8.1 — Create project file

Create `src/Tahaq.Nats/Tahaq.Nats.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>NATS JetStream messaging for Tahaq-based microservices.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NATS.Client.JetStream" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
</Project>
```

#### Step 2.8.2 — Copy and refactor

| Source Path | Destination Path | Namespace |
|-------------|-----------------|-----------|
| `NATSNotificationSystem/INatsService.cs` | `INatsService.cs` | `Tahaq.Nats` |
| `NATSNotificationSystem/NatsConnectionManager.cs` | `NatsConnectionManager.cs` | `Tahaq.Nats` |
| `NATSNotificationSystem/NatsService.cs` | `NatsService.cs` | `Tahaq.Nats` |

#### Step 2.8.3 — Decision point

The NATS implementation is **entirely commented out**. Two options:

**Option A (recommended):** Do not publish this package yet. Create the project structure but mark it as `<Version>1.0.0-alpha</Version>` and do not reference it from any service until the implementation is complete.

**Option B:** Uncomment the existing code, fix it, and publish. This requires understanding the intended NATS usage pattern.

#### Step 2.8.4 — Do NOT copy `NatsDummyProgram.cs`

This is test/example code and should not be in the package.

---

## Phase 3: Build and Pack

### Step 3.1 — Build all packages

```bash
cd tahaq-packages
dotnet build src/Tahaq.Core/Tahaq.Core.csproj
dotnet build src/Tahaq.Data/Tahaq.Data.csproj
dotnet build src/Tahaq.Auth/Tahaq.Auth.csproj
dotnet build src/Tahaq.Redis/Tahaq.Redis.csproj
dotnet build src/Tahaq.Logging/Tahaq.Logging.csproj
dotnet build src/Tahaq.Email/Tahaq.Email.csproj
dotnet build src/Tahaq.Grpc/Tahaq.Grpc.csproj
```

### Step 3.2 — Fix all build errors

Resolve any namespace references, missing usings, or API incompatibilities.

### Step 3.3 — Pack

```bash
dotnet pack --configuration Release --output ./nupkgs
```

This produces `.nupkg` files in `./nupkgs/` for each package.

### Step 3.4 — Publish to feed

**Local folder (for testing):**
```bash
dotnet nuget add source ./nupkgs --name TahaqLocal
```

**GitHub Packages:**
```bash
dotnet nuget push ./nupkgs/*.nupkg --source "github" --api-key $GITHUB_TOKEN
```

**Azure Artifacts:**
```bash
dotnet nuget push ./nupkgs/*.nupkg --source "AzureArtifacts" --api-key az
```

---

## Phase 4: Migrate Tahaq Template

### Step 4.1 — Update `Tahaq/Utility/Utility.csproj`

Replace the entire project with NuGet references. Delete all source files from `Tahaq/Utility/` and replace the `.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Tahaq.Core" Version="1.0.0" />
    <PackageReference Include="Tahaq.Data" Version="1.0.0" />
    <PackageReference Include="Tahaq.Auth" Version="1.0.0" />
    <PackageReference Include="Tahaq.Redis" Version="1.0.0" />
    <PackageReference Include="Tahaq.Logging" Version="1.0.0" />
    <PackageReference Include="Tahaq.Email" Version="1.0.0" />
    <PackageReference Include="Tahaq.Grpc" Version="1.0.0" />
  </ItemGroup>
</Project>
```

Or, better: remove the `Utility` project entirely and reference packages directly from `Core`, `DA`, and `Host` projects as needed.

### Step 4.2 — Update `Tahaq/__ProjectName__.Host/Extensions/Resources.cs`

Replace current DI calls:

```csharp
// BEFORE:
services.AddRedisSessionManagement(configuration);
services.AddCustomLogger(configuration);
services.AddEmailSender(configuration);
services.AddAuthProvider(configuration);

// AFTER:
services.AddTahaqRedis(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("RedisHost")
        ?? throw new InvalidOperationException("RedisHost not configured");
});

services.AddTahaqLogging(options =>
{
    options.LogFilePath = "logs/app-Logs.txt";
    options.SlackWebhookUrl = Environment.GetEnvironmentVariable("SlackWebHook");
    options.AppName = "__ProjectName__";
});

services.AddTahaqEmail(options =>
{
    options.SenderEmail = Environment.GetEnvironmentVariable("SenderEmail") ?? throw new InvalidOperationException();
    options.SenderPassword = Environment.GetEnvironmentVariable("SenderPassword") ?? throw new InvalidOperationException();
    options.SmtpHost = Environment.GetEnvironmentVariable("EmailHost") ?? throw new InvalidOperationException();
    options.SmtpPort = int.Parse(Environment.GetEnvironmentVariable("EmailPort") ?? "587");
});

services.AddTahaqAuth(options =>
{
    options.JwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
        ?? throw new InvalidOperationException("JWT_KEY not configured");
    options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? throw new InvalidOperationException("JWT_ISSUER not configured");
    options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? throw new InvalidOperationException("JWT_AUDIENCE not configured");
});
```

### Step 4.3 — Move domain-specific files to the template

Move these files from `Utility/` into the template's `Core/` or `Host/` project:

- `KConstant.cs` → `__ProjectName__.Host/Constants/KConstant.cs` (set `ApiName = "__ProjectName__"`)
- `KDefinedRoles.cs` → `Core/Constants/KDefinedRoles.cs` (remove hardcoded `AppUserId`)
- `AppConstants.cs` → `Core/Constants/AppConstants.cs` (remove barrier GUIDs)
- `S3Helper.cs` → `Core/Helpers/S3Helper.cs` (only if the template needs S3)
- `DeviceIdHeaderOperationFilter.cs` → `__ProjectName__.Host/Swagger/DeviceIdHeaderOperationFilter.cs`

### Step 4.4 — Update namespace references

Search and replace across the template:
- `using Utility.GenericRepository` → `using Tahaq.Data.Repository`
- `using Utility.EndpointController` → `using Tahaq.Core.Endpoints`
- `using Utility.Helpers.Common` → `using Tahaq.Core.Response` (for ApiResponseModel)
- `using Utility.Helpers.Auth` → `using Tahaq.Auth.Jwt` / `using Tahaq.Auth.Middleware`
- `using Utility.Helpers.Common.Auth` → `using Tahaq.Auth.Authorization`
- `using Utility.Logger` → `using Tahaq.Logging`
- `using Utility.EmailSender` → `using Tahaq.Email`
- `using Utility.SessionManager` → `using Tahaq.Redis`

### Step 4.5 — Update `IFeature` usage

If adopting the static abstract `IFeature`:

```csharp
// BEFORE (instance method):
public class GetGuards : IGuard
{
    public void Map(IEndpointRouteBuilder app) { ... }
}

// AFTER (static abstract):
public class GetGuards : IGuard
{
    public static void Map(IEndpointRouteBuilder app) { ... }
}
```

Update `RegisterFeatures.cs` to use the static method:

```csharp
// BEFORE:
IEnumerable<IFeature> endpoints = app.Services.GetRequiredService<IEnumerable<IFeature>>();
foreach (IFeature endpoint in endpoints)
{
    endpoint.Map(group);
}

// AFTER:
var featureTypes = assembly.DefinedTypes
    .Where(type => type is { IsAbstract: false, IsInterface: false }
        && type.IsAssignableTo(typeof(IFeature)));

foreach (var featureType in featureTypes)
{
    var mapMethod = featureType.GetMethod("Map", BindingFlags.Public | BindingFlags.Static);
    mapMethod?.Invoke(null, new object[] { group });
}
```

### Step 4.6 — Build and verify the template

```bash
cd Tahaq
./setup-template.ps1 -ProjectName TestService
dotnet build TestService.slnx
```

---

## Phase 5: Migrate Oaken (After Template Is Verified)

### Step 5.1 — Add NuGet source to Oaken

```bash
cd Oaken
dotnet nuget add source <feed-url> --name Tahaq
```

### Step 5.2 — Replace Utility project reference

In each `.csproj` that references `Utility`, replace the `<ProjectReference>` with `<PackageReference>` entries for only the packages that project needs.

### Step 5.3 — Keep Oaken-specific code locally

Move these from `Oaken/Utility/` to `Oaken/Core/` or `Oaken/Oaken.Host/`:
- `SignalR/NotificationHub.cs`
- `SignalR/INotificationClient.cs`
- `SignalR/DependencyInjection.cs`
- `Helpers/CommonRoles/RoleConstants.cs`
- `KConstant.cs` (with `ApiName = "Oaken"`)
- `S3Helper.cs`

### Step 5.4 — Update namespaces and DI calls

Same namespace replacements as Phase 4.4. Same DI call updates as Phase 4.2.

### Step 5.5 — Build and test

```bash
dotnet build Oaken.Host.csproj
```

---

## Phase 6: Migrate QI (After Oaken Is Verified)

### Step 6.1 — Remove 11 Utility sub-projects

Delete the following project directories from `QI/Utility/`:
- `AuthProvider/`, `CustomHTTP/`, `EmailSender/`, `EndpointController/`, `EndpointExposerGRPC/`, `GenericRepository/`, `Helpers/`, `Logger/`, `NATSNotificationSystem/`, `SessionManager/`, `UserActivity/`

### Step 6.2 — Add NuGet package references

Update `Host/CSAPI/CSAPI.csproj` and `Infrastructure/CSPInfra/BS/BS.csproj` to reference the Tahaq packages.

### Step 6.3 — Reconcile QI-specific extensions

QI's `GenericRepository` has extra methods (`UpdateOnCondition`, `UpdateOnConditionAsync`). Keep these as local extension methods:

```csharp
// QI/Infrastructure/CSPInfra/DA/Extensions/GenericRepositoryExtensions.cs
public static class GenericRepositoryExtensions
{
    public static async Task<(bool, string)> UpdateOnConditionAsync<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repo, ...)
    {
        // QI-specific implementation
    }
}
```

### Step 6.4 — Adopt static abstract IFeature

Update all QI feature files (already using static abstract — no change needed if QI's `IFeature` matches the package's).

### Step 6.5 — Update namespaces

QI uses different namespaces (no `Utility.` prefix). Search and replace:
- `using GenericRepository` → `using Tahaq.Data.Repository`
- `using Helpers.Auth` → `using Tahaq.Auth`
- `using EndpointController` → `using Tahaq.Core.Endpoints`
- etc.

### Step 6.6 — Move `ApiResponseModel` and `CustomAuthorizationHandler`

QI has these in `Host/CSAPI/Common/`. Either:
- Delete them and use the package versions, or
- Keep them if they've diverged and the QI team prefers their versions

### Step 6.7 — Build and test

```bash
dotnet build Host/CSAPI/CSAPI.csproj
```

---

## Validation Checklist

After each phase, verify:

- [ ] `dotnet build` succeeds with zero errors
- [ ] No `using Utility.*` namespaces remain (replaced with `Tahaq.*`)
- [ ] No hardcoded secrets in the packages
- [ ] No `Console.WriteLine` in package code
- [ ] No commented-out code in packages
- [ ] All DI registration uses options pattern (no direct `Environment.GetEnvironmentVariable` in packages)
- [ ] `IFeature` interface is consistent across all services
- [ ] JWT validation has `ValidateIssuer = true` and `ValidateAudience = true`
- [ ] GenericRepository does not leak exception messages
