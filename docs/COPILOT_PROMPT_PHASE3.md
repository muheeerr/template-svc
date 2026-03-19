# GitHub Copilot Prompt — template-svc Remediation
## Phase 3: Enterprise Capabilities
> Model: `claude-opus-4-6` | Project: `projectname` | Stack: .NET 10 / ASP.NET Core Minimal API

---

## PREREQUISITE CHECK

Before starting any task in this phase, confirm the following are already done:

- [ ] Phase 1 complete — all 10 security fixes applied, `dotnet build` passes
- [ ] Phase 2 complete — domain code removed, Infrastructure project exists, dead code cleared
- [ ] `launchSettings.json` contains all required env vars from Phase 1 & 2
- [ ] No `?? "hardcoded-fallback"` patterns remain for any secret or connection string

If any of the above are not done, stop and complete Phase 1 & 2 first using `COPILOT_PROMPT_PHASE1_2.md`.

---

## STANDING RULES FOR PHASE 3

**Rule 1 — No Console.WriteLine.**
Every new file you write must use `ILogger<T>`. No `Console.WriteLine` anywhere. If you encounter one while editing a file, replace it with the appropriate `_logger.LogInformation(...)` call before moving on.

**Rule 2 — No new hardcoded values.**
Any new URL, connection string, timeout, limit, or configuration value must come from `IConfiguration` or environment variables and have a sensible entry added to `launchSettings.json`.

**Rule 3 — New env vars go to launchSettings.json immediately.**
Every task below that introduces a new env var lists it explicitly. Add it to `launchSettings.json` before writing the code that consumes it.

**Rule 4 — `dotnet build` after every task.**
Do not proceed to the next task if the build fails.

**Rule 5 — Test project has its own build verification.**
After Task 21, also run `dotnet test` at the end of every subsequent task to confirm no tests are broken by changes.

---

## New `launchSettings.json` entries required across Phase 3

Add all of these upfront so they are available as tasks reference them:

```json
{
  "profiles": {
    "Development": {
      "environmentVariables": {
        "HEALTH_CHECK_PATH":     "/health",
        "HEALTH_READY_PATH":     "/health/ready",
        "HEALTH_LIVE_PATH":      "/health/live",
        "SLACK_WEBHOOK_URL":     "",
        "SMTP_HOST":             "localhost",
        "SMTP_PORT":             "1025",
        "OTEL_EXPORTER_ENDPOINT":"http://localhost:4317",
        "OTEL_SERVICE_NAME":     "projectname",
        "OTEL_SERVICE_VERSION":  "1.0.0",
        "SEED_DATABASE":         "true",
        "DEFAULT_API_VERSION":   "1.0",
        "COMPRESSION_LEVEL":     "Optimal"
      }
    }
  }
}
```

---

## TASK 21 — Add xUnit Test Project with WebApplicationFactory

**Effort:** Large | **New files:** `Tests/projectname.Tests.csproj`, `Tests/Common/`, `Tests/Features/`

### Step 1 — Create the test project

```bash
dotnet new xunit -n projectname.Tests -o Tests
dotnet sln add Tests/projectname.Tests.csproj
dotnet add Tests/projectname.Tests.csproj reference projectname.Host/projectname.Host.csproj
```

### Step 2 — Add NuGet packages

```bash
cd Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.PostgreSql
dotnet add package Testcontainers.Redis
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package coverlet.collector
```

### Step 3 — Create `Tests/Common/TestWebAppFactory.cs`

This is the shared factory for all integration tests. It spins up real PostgreSQL and Redis via Testcontainers so tests run against actual infrastructure, not mocks:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using DA.Persistence;

namespace projectname.Tests.Common;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real DB with Testcontainers DB
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));

            // Override env vars for test environment
            Environment.SetEnvironmentVariable("DBHost",       _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("JWT_KEY",      "test-jwt-key-minimum-32-characters!!");
            Environment.SetEnvironmentVariable("JWT_ISSUER",   "https://test-issuer");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
            Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "https://localhost:3000");
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
```

### Step 4 — Create `Tests/Common/TestAuthHelper.cs`

Helper to generate test JWTs so integration tests can hit authenticated endpoints:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace projectname.Tests.Common;

public static class TestAuthHelper
{
    public static string GenerateTestJwt(
        string userId   = "test-user-id",
        string userType = "Admin",
        string email    = "test@test.com")
    {
        var key   = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("test-jwt-key-minimum-32-characters!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("UserId",   userId),
            new Claim("UserType", userType),
            new Claim("Email",    email),
        };

        var token = new JwtSecurityToken(
            issuer:             "https://test-issuer",
            audience:           "test-audience",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Step 5 — Create `Tests/Features/HealthCheckTests.cs`

First integration test — validates the health endpoint works end-to-end:

```csharp
using FluentAssertions;
using projectname.Tests.Common;

namespace projectname.Tests.Features;

public class HealthCheckTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
```

### Step 6 — Create `Tests/Features/ExampleFeatureTests.cs`

Integration test for the example feature endpoint with auth:

```csharp
using System.Net.Http.Headers;
using FluentAssertions;
using projectname.Tests.Common;

namespace projectname.Tests.Features;

public class ExampleFeatureTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetExample_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/Example/GetExample");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExample_WithValidToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHelper.GenerateTestJwt());

        var response = await _client.GetAsync("/Example/GetExample");
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

### Step 7 — Verify

```bash
dotnet build
dotnet test Tests/ --logger "console;verbosity=normal"
```

---

## TASK 22 — Add Docker Compose for Local Development

**Effort:** Medium | **New file:** `docker-compose.yml` (project root)

Create a `docker-compose.yml` at the solution root that spins up all infrastructure dependencies so developers can run the service locally with one command:

```yaml
version: "3.9"

services:

  postgres:
    image: postgres:16-alpine
    container_name: projectname-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB:       projectnameDev
      POSTGRES_USER:     postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: projectname-redis
    restart: unless-stopped
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  mailpit:
    image: axllent/mailpit:latest
    container_name: projectname-mailpit
    restart: unless-stopped
    ports:
      - "1025:1025"   # SMTP
      - "8025:8025"   # Web UI at http://localhost:8025
    environment:
      MP_MAX_MESSAGES: 100
      MP_DATABASE:     /data/mailpit.db
    volumes:
      - mailpit_data:/data

  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    container_name: projectname-otel
    restart: unless-stopped
    command: ["--config=/etc/otel-collector.yaml"]
    volumes:
      - ./otel-collector.yaml:/etc/otel-collector.yaml
    ports:
      - "4317:4317"   # gRPC
      - "4318:4318"   # HTTP

volumes:
  postgres_data:
  redis_data:
  mailpit_data:
```

Also create `otel-collector.yaml` at the root (minimal config for local OTEL collection):

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

exporters:
  debug:
    verbosity: detailed

service:
  pipelines:
    traces:
      receivers:  [otlp]
      exporters:  [debug]
    metrics:
      receivers:  [otlp]
      exporters:  [debug]
    logs:
      receivers:  [otlp]
      exporters:  [debug]
```

Add a usage comment at the top of `docker-compose.yml`:

```yaml
# Usage:
#   docker compose up -d          → start all services
#   docker compose down           → stop all services
#   docker compose down -v        → stop and delete volumes
#   Mailpit UI: http://localhost:8025
#   PostgreSQL: localhost:5432
#   Redis:      localhost:6379
```

Update `launchSettings.json` SMTP settings to point to Mailpit:
```json
"SMTP_HOST": "localhost",
"SMTP_PORT": "1025"
```

---

## TASK 23 — Add GitHub Actions CI/CD Pipeline

**Effort:** Medium | **New files:** `.github/workflows/ci.yml`, `.github/workflows/cd.yml`

### `.github/workflows/ci.yml` — runs on every push and PR

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: "10.0.x"

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB:       testdb
          POSTGRES_USER:     testuser
          POSTGRES_PASSWORD: testpass
        ports: ["5432:5432"]
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

      redis:
        image: redis:7-alpine
        ports: ["6379:6379"]
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"
        env:
          DBHost:        "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass"
          JWT_KEY:       ${{ secrets.JWT_KEY_TEST }}
          JWT_ISSUER:    "https://ci-issuer"
          JWT_AUDIENCE:  "ci-audience"
          ALLOWED_ORIGINS: "https://localhost:3000"
          SenderEmail:   "ci@test.com"
          SenderPassword: "ci-pass"
          AES_KEY:       ${{ secrets.AES_KEY_TEST }}

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/test-results.trx"

      - name: Upload Coverage
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: coverage
          path: "**/coverage.cobertura.xml"

  code-quality:
    name: Code Quality
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Install dotnet-format
        run: dotnet tool install -g dotnet-format
      - name: Check formatting
        run: dotnet format --verify-no-changes --severity warn
```

### `.github/workflows/cd.yml` — deploys on push to main

```yaml
name: CD

on:
  push:
    branches: [main]
    tags:     ["v*"]

env:
  REGISTRY:   ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    name: Build & Push Docker Image
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags:   |
            type=semver,pattern={{version}}
            type=sha,prefix=sha-
            type=raw,value=latest,enable=${{ github.ref == 'refs/heads/main' }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context:   .
          file:      ./projectname.Host/Dockerfile
          push:      true
          tags:      ${{ steps.meta.outputs.tags }}
          labels:    ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to:   type=gha,mode=max
```

Add required secrets to GitHub repository settings:
- `JWT_KEY_TEST` — any 32+ char test key
- `AES_KEY_TEST` — any 32+ char test AES key

---

## TASK 24 — Add Comprehensive Health Checks (DB, Redis)

**Effort:** Medium | **Files:** `Host/Extensions/Resources.cs`, `Host/Extensions/ConfigureApp.cs`

### Step 1 — Add NuGet packages to Host project

```bash
dotnet add projectname.Host package AspNetCore.HealthChecks.NpgSql
dotnet add projectname.Host package AspNetCore.HealthChecks.Redis
dotnet add projectname.Host package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
```

### Step 2 — Register health checks in `Resources.cs`

```csharp
var connectionString = Environment.GetEnvironmentVariable("DBHost")
    ?? throw new InvalidOperationException("DBHost is required.");
var redisConnection  = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
    ?? "localhost:6379";

services
    .AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name:         "postgresql",
        tags:         ["db", "ready"],
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
    .AddRedis(
        redisConnection,
        name:         "redis",
        tags:         ["cache", "ready"],
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    .AddDbContextCheck<AppDbContext>(
        name:         "ef-core",
        tags:         ["db", "ready"]);
```

Add `REDIS_CONNECTION` to `launchSettings.json`:
```json
"REDIS_CONNECTION": "localhost:6379"
```

### Step 3 — Map health endpoints in `ConfigureApp.cs`

Replace the existing simple `/health` endpoint (or the one in `Health.cs`) with split endpoints:

```csharp
// Liveness — is the process alive? No dependency checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate      = _ => false,   // no checks, just returns 200 if process is up
    ResponseWriter = WriteHealthResponse
});

// Readiness — are all dependencies reachable?
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate      = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

// Full health — everything, for internal dashboards only
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse
}).RequireAuthorization();  // internal only
```

Add the response writer so health output is JSON, not plain text:

```csharp
private static Task WriteHealthResponse(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = JsonSerializer.Serialize(new
    {
        status     = report.Status.ToString(),
        duration   = report.TotalDuration.TotalMilliseconds,
        checks     = report.Entries.Select(e => new
        {
            name     = e.Key,
            status   = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            error    = e.Value.Exception?.Message
        })
    });
    return ctx.Response.WriteAsync(result);
}
```

---

## TASK 25 — Add API Versioning Strategy

**Effort:** Medium | **Files:** `Host/Extensions/Resources.cs`, `Core/Endpoints/RegisterFeatures.cs`

### Step 1 — Add NuGet package

```bash
dotnet add projectname.Host package Asp.Versioning.Http
dotnet add Core package Asp.Versioning.Http
```

### Step 2 — Register versioning in `Resources.cs`

```csharp
services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion                = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions               = true;  // adds api-supported-versions header
        options.ApiVersionReader                = ApiVersionReader.Combine(
            new HeaderApiVersionReader("X-Api-Version"),   // via header
            new QueryStringApiVersionReader("api-version") // via ?api-version=1.0
        );
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat           = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
```

### Step 3 — Apply versioning to feature routes in `RegisterFeatures.cs`

Wrap the existing `MapGroup` call to include version prefix:

```csharp
// Before: var group = builder.MapGroup($"/{featureInterfaceName}");

// After: versioned group
var versionSet = builder.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var group = builder
    .MapGroup($"/v{{version:apiVersion}}/{featureInterfaceName}")
    .WithApiVersionSet(versionSet);
```

This changes routes from `/Employee/Get` to `/v1/Employee/Get`. Update `ExampleFeatureTests.cs` URLs accordingly.

### Step 4 — Update health check paths (they are not versioned)

Health check endpoints stay at `/health/*` without version prefix — they are infrastructure concerns, not API concerns.

---

## TASK 26 — Implement Retry / Circuit Breaker with Polly

**Effort:** Medium | **Files:** `Infrastructure/` DI registrations, `Host/Extensions/Resources.cs`

### Step 1 — Add NuGet packages

```bash
dotnet add projectname.Host package Microsoft.Extensions.Http.Resilience
dotnet add Infrastructure package Microsoft.Extensions.Http.Resilience
```

### Step 2 — Add standard resilience pipeline for all outbound HTTP clients

In `Resources.cs` (or `Infrastructure/*/DependencyInjection.cs`), apply the standard resilience handler to all named `HttpClient` registrations:

```csharp
// Apply to the Slack HTTP client registered in Phase 2
services.AddHttpClient("slack")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts       = 3;
        options.Retry.Delay                  = TimeSpan.FromSeconds(1);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio  = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.TotalRequestTimeout.Timeout  = TimeSpan.FromSeconds(15);
    });

// Apply to any other external HTTP clients (auth service, etc.)
services.AddHttpClient("external-auth")
    .AddStandardResilienceHandler();
```

### Step 3 — Add retry for EF Core database operations

In `DA/DependencyInjection.cs`, enable EF Core execution strategy for transient PostgreSQL failures:

```csharp
services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString, npgsqlOpts =>
        npgsqlOpts.EnableRetryOnFailure(
            maxRetryCount:       5,
            maxRetryDelay:       TimeSpan.FromSeconds(10),
            errorCodesToAdd:     null)));
```

### Step 4 — Add Redis resilience

Wrap Redis operations in a try-catch with degraded-mode fallback in `RedisSessionManager`:

```csharp
public async Task<T?> GetAsync<T>(string key)
{
    try
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }
    catch (RedisException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable for key {Key}. Returning default.", key);
        return default;  // graceful degradation — don't crash the request
    }
}
```

---

## TASK 27 — Complete OpenTelemetry Configuration

**Effort:** Medium | **File:** `Host/Extensions/OpenTelemetryExtensions.cs`

The existing OTEL setup is partial. Complete it so traces, metrics, and logs all flow to the collector:

### Step 1 — Add missing NuGet packages

```bash
dotnet add projectname.Host package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add projectname.Host package OpenTelemetry.Instrumentation.AspNetCore
dotnet add projectname.Host package OpenTelemetry.Instrumentation.Http
dotnet add projectname.Host package OpenTelemetry.Instrumentation.EntityFrameworkCore
dotnet add projectname.Host package OpenTelemetry.Instrumentation.StackExchangeRedis
```

### Step 2 — Rewrite `OpenTelemetryExtensions.cs`

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services, IConfiguration config)
    {
        var serviceName    = config["OTEL_SERVICE_NAME"]    ?? "projectname";
        var serviceVersion = config["OTEL_SERVICE_VERSION"] ?? "1.0.0";
        var endpoint       = config["OTEL_EXPORTER_ENDPOINT"] ?? "http://localhost:4317";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException         = true;
                    opts.Filter                  = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(opts => opts.RecordException = true)
                .AddEntityFrameworkCoreInstrumentation(opts =>
                    opts.SetDbStatementForText = true)  // dev only — disable in prod
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(endpoint)))

            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(endpoint)));

        return services;
    }
}
```

### Step 3 — Wire into `Program.cs`

Replace the existing partial OTEL call with:

```csharp
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

// Add OTEL logging provider
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "projectname"));
    logging.AddOtlpExporter(opts =>
        opts.Endpoint = new Uri(
            builder.Configuration["OTEL_EXPORTER_ENDPOINT"] ?? "http://localhost:4317"));
});
```

---

## TASK 28 — Add Structured Logging with Correlation IDs

**Effort:** Medium | **Files:** `Host/Middlewares/`, `Host/Extensions/Resources.cs`

### Step 1 — Create `Host/Middlewares/CorrelationIdMiddleware.cs`

```csharp
namespace projectname.Host.Middlewares;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("RequestPath",   context.Request.Path))
        using (Serilog.Context.LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await next(context);
        }
    }
}
```

### Step 2 — Register in `ConfigureApp.cs`

Add immediately after `UseCors()` and before `GlobalExceptionHandlerMiddleware`:

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

### Step 3 — Enrich Serilog in `Program.cs`

Update the Serilog configuration to include correlation ID enrichment and structured output:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()           // picks up CorrelationId pushed by middleware
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service",   builder.Configuration["OTEL_SERVICE_NAME"] ?? "projectname")
    .Enrich.WithProperty("Version",   builder.Configuration["OTEL_SERVICE_VERSION"] ?? "1.0.0")
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())  // structured JSON
    .CreateLogger();
```

Add `Serilog.Enrichers.Environment` package:
```bash
dotnet add projectname.Host package Serilog.Enrichers.Environment
dotnet add projectname.Host package Serilog.Formatting.Compact
```

### Step 4 — Replace all remaining `Console.WriteLine` across the solution

Run this to find them:
```bash
grep -rn "Console.WriteLine" --include="*.cs" .
```

Replace every occurrence with the appropriate `ILogger` call. For DI registration files where `ILogger` is not yet available, use `ILogger` via `IServiceProvider` post-build, or simply remove the registration logs entirely — the DI framework does not need them.

---

## TASK 29 — Enable EF Core Migrations Properly

**Effort:** Medium | **Files:** `DA/DependencyInjection.cs`, `Program.cs`, `DA/Migrations/`

### Step 1 — Enable migrations

```bash
cd DA
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate --startup-project ../projectname.Host --output-dir Migrations
```

### Step 2 — Replace `EnsureDatabaseCreated` in `Program.cs`

The existing method is empty. Replace it with a proper migration runner:

```csharp
private static async Task ApplyMigrationsAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed. Application cannot start.");
        throw;  // fail fast — don't start with a broken schema
    }
}
```

Call it in `Program.cs` after `app.Build()` and before `app.Run()`:

```csharp
var app = builder.Build();
await ApplyMigrationsAsync(app);  // ← add this line
app.ConfigureApp();
await app.RunAsync();
```

### Step 3 — Add migration instructions to README

Add a section to `README.md` (create it if it doesn't exist):

```markdown
## Database Migrations

Add a new migration:
```bash
dotnet ef migrations add <MigrationName> \
  --project DA \
  --startup-project projectname.Host
```

Apply migrations manually (runs automatically on startup in Development):
```bash
dotnet ef database update \
  --project DA \
  --startup-project projectname.Host
```

Roll back last migration:
```bash
dotnet ef migrations remove \
  --project DA \
  --startup-project projectname.Host
```
```

---

## TASK 30 — Add Response Compression Middleware

**Effort:** Small | **Files:** `Host/Extensions/Resources.cs`, `Host/Extensions/ConfigureApp.cs`

### Step 1 — Register in `Resources.cs`

```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;  // safe since we control the content
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/grpc"
    });
});

services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal);
services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.SmallestSize);
```

### Step 2 — Add to pipeline in `ConfigureApp.cs`

Add immediately after `UseHttpsRedirection()`:

```csharp
app.UseResponseCompression();
```

Verify with a curl call that the response includes `Content-Encoding: br` or `Content-Encoding: gzip` when the client sends `Accept-Encoding: br, gzip`.

---

## TASK 31 — Unify Serialization to System.Text.Json

**Effort:** Medium | **Files:** `Utility/Helpers/Common/ApiResponse/ApiResponseModel.cs`, `Resources.cs`

### Step 1 — Find all Newtonsoft.Json usages

```bash
grep -rn "Newtonsoft.Json\|JsonConvert\|CamelCasePropertyNamesContractResolver" --include="*.cs" .
```

### Step 2 — Replace in `ApiResponseModel.ExecuteAsync()`

```csharp
// ❌ Before — Newtonsoft
using Newtonsoft.Json;
var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
});

// ✅ After — System.Text.Json
using System.Text.Json;
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    WriteIndented               = false
};

var json = JsonSerializer.Serialize(this, _jsonOptions);
```

### Step 3 — Configure System.Text.Json globally in `Resources.cs`

```csharp
services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
```

### Step 4 — Remove Newtonsoft.Json from project references

After replacing all usages, remove the package:

```bash
dotnet remove Utility package Newtonsoft.Json
dotnet build  # verify nothing breaks
```

---

## TASK 32 — Refactor `Repository.cs` (700+ lines → Specification Pattern)

**Effort:** Large | **Files:** `DA/Persistence/Repository/Repository.cs`, new `DA/Specifications/`

### Step 1 — Create the Specification base

```csharp
// DA/Specifications/BaseSpecification.cs
namespace DA.Specifications;

public abstract class BaseSpecification<T>
{
    public Expression<Func<T, bool>>?          Criteria       { get; protected set; }
    public List<Expression<Func<T, object>>>   Includes       { get; } = [];
    public List<string>                        IncludeStrings { get; } = [];
    public Expression<Func<T, object>>?        OrderBy        { get; protected set; }
    public Expression<Func<T, object>>?        OrderByDesc    { get; protected set; }
    public int                                 Take           { get; protected set; }
    public int                                 Skip           { get; protected set; }
    public bool                                IsPagingEnabled { get; protected set; }

    protected void AddInclude(Expression<Func<T, object>> include)
        => Includes.Add(include);

    protected void ApplyPaging(int pageIndex, int pageSize)
    {
        Skip           = (pageIndex - 1) * pageSize;
        Take           = pageSize;
        IsPagingEnabled = true;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc)
        => OrderByDesc = orderByDesc;
}
```

### Step 2 — Create `DA/Specifications/SpecificationEvaluator.cs`

```csharp
namespace DA.Specifications;

public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> query, BaseSpecification<T> spec)
    {
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (q, i) => q.Include(i));
        query = spec.IncludeStrings.Aggregate(query, (q, i) => q.Include(i));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDesc is not null)
            query = query.OrderByDescending(spec.OrderByDesc);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }
}
```

### Step 3 — Add spec-based methods to `IRepository.cs`

```csharp
Task<T?>              GetBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default);
Task<IReadOnlyList<T>> ListBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default);
Task<int>             CountBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default);
```

### Step 4 — Implement in `Repository.cs`

```csharp
public async Task<T?> GetBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default)
    => await SpecificationEvaluator<T>.GetQuery(_context.Set<T>(), spec).FirstOrDefaultAsync(ct);

public async Task<IReadOnlyList<T>> ListBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default)
    => await SpecificationEvaluator<T>.GetQuery(_context.Set<T>(), spec).ToListAsync(ct);

public async Task<int> CountBySpecAsync(BaseSpecification<T> spec, CancellationToken ct = default)
    => await SpecificationEvaluator<T>.GetQuery(_context.Set<T>(), spec).CountAsync(ct);
```

### Step 5 — Consolidate the 30+ methods

Group the 30+ repository methods into these focused categories and remove duplication:

- **CRUD**: `GetByIdAsync`, `ListAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- **Soft-delete**: `SoftDeleteAsync`, `RestoreAsync`
- **Specification**: `GetBySpecAsync`, `ListBySpecAsync`, `CountBySpecAsync`
- **Pagination**: one single `GetPagedAsync(BaseSpecification<T> spec, int page, int size)` — remove the 6 separate pagination overloads

Target: reduce from 700+ lines to under 250 lines.

---

## TASK 33 — Replace All `Console.WriteLine` with `ILogger`

**Effort:** Medium | **Files:** All `DependencyInjection.cs` files across all projects

This task is a sweep of what was not caught in Task 28. Run again to confirm all are gone:

```bash
grep -rn "Console.WriteLine" --include="*.cs" .
```

For DI registration files where `ILogger<T>` is not yet injectable (because the container isn't built yet), remove the `Console.WriteLine` calls entirely — the startup validation from Phase 1 Task 4 and OTEL from Task 27 already provide sufficient startup observability.

If a meaningful startup message must be kept, use `app.Logger` after `app.Build()` in `Program.cs`:

```csharp
var app = builder.Build();
app.Logger.LogInformation("{Service} starting up in {Environment}",
    app.Configuration["OTEL_SERVICE_NAME"],
    app.Environment.EnvironmentName);
```

---

## TASK 34 — Add Database Seed Data Mechanism

**Effort:** Medium | **New files:** `DA/Seeding/IDataSeeder.cs`, `DA/Seeding/ExampleDataSeeder.cs`

### Step 1 — Create the seeder interface

```csharp
// DA/Seeding/IDataSeeder.cs
namespace DA.Seeding;

public interface IDataSeeder
{
    int Order { get; }  // Lower = runs first
    Task SeedAsync(CancellationToken ct = default);
}
```

### Step 2 — Create an example seeder

```csharp
// DA/Seeding/ExampleDataSeeder.cs
namespace DA.Seeding;

public class ExampleDataSeeder(AppDbContext db, ILogger<ExampleDataSeeder> logger) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Example: only seed if table is empty
        if (await db.Set<ExampleEntity>().AnyAsync(ct))
        {
            logger.LogInformation("ExampleEntity already has data — skipping seed.");
            return;
        }

        // Add seed data here
        // db.Set<ExampleEntity>().Add(new ExampleEntity { ... });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("ExampleEntity seed data applied.");
    }
}
```

### Step 3 — Register seeders via assembly scan in `DA/DependencyInjection.cs`

```csharp
// Auto-register all IDataSeeder implementations
var seederTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IDataSeeder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

foreach (var seederType in seederTypes)
    services.AddTransient(typeof(IDataSeeder), seederType);
```

### Step 4 — Run seeders in `Program.cs` after migrations

```csharp
private static async Task RunSeedersAsync(WebApplication app)
{
    var seedDatabase = bool.TryParse(
        Environment.GetEnvironmentVariable("SEED_DATABASE"), out var val) && val;

    if (!seedDatabase) return;

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Running database seeders...");

    using var scope = app.Services.CreateScope();
    var seeders = scope.ServiceProvider
        .GetServices<IDataSeeder>()
        .OrderBy(s => s.Order);

    foreach (var seeder in seeders)
        await seeder.SeedAsync();

    logger.LogInformation("Database seeding complete.");
}
```

Call it in `Program.cs` after migrations:

```csharp
await ApplyMigrationsAsync(app);
await RunSeedersAsync(app);
```

`SEED_DATABASE` is already in `launchSettings.json` (added at the top of this prompt). Set it to `false` in production.

---

## TASK 35 — Add Kubernetes Manifests (Helm Chart)

**Effort:** Large | **New directory:** `helm/projectname/`

### Step 1 — Create Helm chart scaffold

```bash
helm create helm/projectname
```

### Step 2 — Update `helm/projectname/values.yaml`

Replace the generated defaults with service-specific values:

```yaml
replicaCount: 2

image:
  repository: ghcr.io/your-org/projectname
  pullPolicy: IfNotPresent
  tag: "latest"

service:
  type: ClusterIP
  port: 80
  targetPort: 8080

ingress:
  enabled:     true
  className:   "nginx"
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: projectname.yourdomain.com
      paths:
        - path:     /
          pathType: Prefix

resources:
  requests:
    cpu:    "100m"
    memory: "256Mi"
  limits:
    cpu:    "500m"
    memory: "512Mi"

livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 15
  periodSeconds:       20

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds:       10

autoscaling:
  enabled:                          true
  minReplicas:                      2
  maxReplicas:                      10
  targetCPUUtilizationPercentage:   70

env:
  ASPNETCORE_ENVIRONMENT: "Production"
  OTEL_SERVICE_NAME:      "projectname"

secretRefs:
  - name: projectname-secrets   # K8s Secret containing JWT_KEY, DBHost, etc.
```

### Step 3 — Update `helm/projectname/templates/deployment.yaml`

In the container spec, reference the secret:

```yaml
envFrom:
  - secretRef:
      name: {{ .Values.secretRefs[0].name }}
env:
  {{- range $key, $val := .Values.env }}
  - name:  {{ $key }}
    value: {{ $val | quote }}
  {{- end }}
```

### Step 4 — Create the Kubernetes Secret template

```yaml
# helm/projectname/templates/secret.yaml
# NOTE: In production, populate this via external-secrets or sealed-secrets.
# Do NOT commit real values here.
apiVersion: v1
kind: Secret
metadata:
  name: {{ index .Values.secretRefs 0 "name" }}
  labels:
    {{- include "projectname.labels" . | nindent 4 }}
type: Opaque
data:
  JWT_KEY:         {{ required "jwt.key is required" .Values.secrets.jwtKey       | b64enc }}
  DBHost:          {{ required "db.host is required" .Values.secrets.dbHost        | b64enc }}
  SenderEmail:     {{ required "email is required"   .Values.secrets.senderEmail   | b64enc }}
  SenderPassword:  {{ required "pass is required"    .Values.secrets.senderPassword | b64enc }}
  AES_KEY:         {{ required "aes.key is required" .Values.secrets.aesKey        | b64enc }}
```

### Step 5 — Add deploy instructions to README

```markdown
## Kubernetes Deployment

Install with Helm:
```bash
helm upgrade --install projectname ./helm/projectname \
  --namespace projectname \
  --create-namespace \
  --set secrets.jwtKey="<your-jwt-key>" \
  --set secrets.dbHost="<your-pg-conn-string>" \
  --set secrets.senderEmail="<your-email>" \
  --set secrets.senderPassword="<your-smtp-password>" \
  --set secrets.aesKey="<your-aes-key>"
```

Lint the chart before deploying:
```bash
helm lint ./helm/projectname
helm template projectname ./helm/projectname --debug
```
```

---

## PHASE 3 COMPLETE CHECKLIST

> Run `dotnet build && dotnet test` before ticking any box.

### Testing
- [ ] xUnit test project created and added to solution
- [ ] `TestWebAppFactory` with Testcontainers (PostgreSQL + Redis) working
- [ ] `TestAuthHelper` generates valid test JWTs
- [ ] Health check integration tests pass
- [ ] Example feature integration tests pass (authenticated + unauthenticated)

### DevOps
- [ ] `docker-compose.yml` starts PostgreSQL, Redis, Mailpit, OTEL Collector
- [ ] `otel-collector.yaml` in project root
- [ ] GitHub Actions CI workflow runs build + tests on push/PR
- [ ] GitHub Actions CD workflow builds and pushes Docker image on main
- [ ] Dockerfile is free of hardcoded names (done in Phase 2 — verify)

### Observability
- [ ] Health checks: `/health/live`, `/health/ready`, `/health` (authenticated)
- [ ] PostgreSQL, Redis, EF Core all checked in `/health/ready`
- [ ] OTEL traces, metrics, and logs all wired to OTLP exporter
- [ ] Correlation ID middleware injects `X-Correlation-ID` on every response
- [ ] Serilog outputs structured JSON with `CorrelationId` field
- [ ] Zero `Console.WriteLine` remaining in solution

### API Quality
- [ ] API versioning active — routes now `/v1/...`
- [ ] Response compression returning `Content-Encoding: br` or `gzip`
- [ ] All serialization uses `System.Text.Json` — Newtonsoft.Json removed
- [ ] `Repository.cs` reduced from 700+ lines to under 250 via Specification pattern

### Resilience
- [ ] Polly retry + circuit breaker on all outbound HTTP clients
- [ ] EF Core `EnableRetryOnFailure` configured
- [ ] Redis operations fail gracefully with logged warning, not exception

### Data
- [ ] EF Core migrations directory exists with `InitialCreate`
- [ ] `ApplyMigrationsAsync` runs on startup
- [ ] `IDataSeeder` / `ExampleDataSeeder` pattern in place
- [ ] `SEED_DATABASE=true` in Development, `false` in production

### Infrastructure
- [ ] Helm chart scaffold created under `helm/projectname/`
- [ ] `values.yaml` has liveness/readiness probes pointing to `/health/live` and `/health/ready`
- [ ] K8s Secret template uses Helm required — won't install without secrets
- [ ] README updated with migration and deployment commands

---

## EXECUTION RULES FOR COPILOT

1. **Work one task at a time.** Confirm build passes before starting the next.
2. **Run `dotnet build && dotnet test` after Task 21 and every task thereafter.**
3. **No `Console.WriteLine`.** Replace any you encounter while editing a file.
4. **No hardcoded values.** Every new config value goes to `launchSettings.json`.
5. **Health check paths are NOT versioned.** `/health/*` stays without `/v1/` prefix.
6. **Do not remove or modify** `ExampleFeatures.cs`, `RegisterFeatures.cs`, `UnitOfWork.cs`, `AppDbContext.cs`, `BaseContext.cs`, or `GlobalExceptionHandlerMiddleware.cs` unless the task explicitly requires it.
7. **Helm chart secrets are placeholders only.** Never commit actual values. Always use `--set` at deploy time or external-secrets in production.

---

*Generated from architecture review of `template-svc` — .NET 10 ASP.NET Core Minimal API*
*Phases 1 & 2 (Security + Architecture) must be complete before executing this prompt.*

