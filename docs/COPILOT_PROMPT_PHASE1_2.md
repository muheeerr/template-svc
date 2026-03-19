# GitHub Copilot Prompt — template-svc Remediation
## Phase 1 (Critical Security) + Phase 2 (Architectural Cleanup)
> Model: `claude-opus-4-6` | Project: `projectname` | Stack: .NET 10 / ASP.NET Core Minimal API

---

## CONTEXT

You are working on an ASP.NET Core Minimal API template called `template-svc`. It targets .NET 10 and uses PostgreSQL, Redis, and gRPC. The solution structure is:

```
projectname.Host      ← Entry point, middleware, DI composition root
Core                      ← Features, endpoints, business layer DI
DA                        ← Data access (EF Core, Repository, UnitOfWork)
Utility                   ← Shared helpers (Auth, JWT, AES, Email, Redis, S3, NATS, etc.)
Infrastructure/           ← EXISTS but is EMPTY
```

A security and architecture review has identified **critical issues** that must be resolved before this template can be used for any production project. You will work through these issues in strict order across two phases.

**Do not start Phase 2 until every task in Phase 1 is complete and verified.**
**Do not touch Phase 3 items in this session.**

---

## STANDING RULE — CREDENTIALS & SECRETS (Apply to every task)

> **Before writing a single line of fix code, apply this rule globally first.**

All hardcoded credentials, secrets, keys, and connection strings currently scattered across the codebase MUST be extracted and placed as environment variable entries in `projectname.Host/Properties/launchSettings.json` under the `environmentVariables` section of the appropriate profile.

### Credentials to move to `launchSettings.json`:

| Variable Name | Source File | Current Hardcoded Value (to REMOVE) |
|---|---|---|
| `JWT_KEY` | `Host/Extensions/Resources.cs` | `"asdavvasd132132131231232312312dsadasdsdsdsds@asd112"` |
| `JWT_ISSUER` | `Host/Extensions/Resources.cs` | (any hardcoded default) |
| `JWT_AUDIENCE` | `Host/Extensions/Resources.cs` | (any hardcoded default) |
| `DBHost` | `DA/DependencyInjection.cs` | `"Host=localhost;Port=5432;Database=projectnameDb;Username=postgres;Password=postgres"` |
| `SenderEmail` | `Utility/EmailSender/DependencyInjection.cs` | `"NA@NA.com"` |
| `SenderPassword` | `Utility/EmailSender/DependencyInjection.cs` | `"NA"` |
| `AES_KEY` | `Utility/AuthProvider/AESEncryption/CustomAESEncryption.cs` | (any static key or IV from config) |
| `SCALAR_BEARER_TOKEN` | `Host/Extensions/ConfigureApp.cs` | (the hardcoded JWT in `.AddHttpAuthentication`) |
| `ALLOWED_ORIGINS` | `Host/Extensions/Resources.cs` + `ConfigureApp.cs` | `"*"` / AllowAnyOrigin |

### Required `launchSettings.json` shape:

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "https://localhost:7087;http://localhost:7088",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "JWT_KEY": "dev-only-change-this-key-min-32-chars!!",
        "JWT_ISSUER": "https://localhost:7087",
        "JWT_AUDIENCE": "template-svc-dev",
        "DBHost": "Host=localhost;Port=5432;Database=projectnameDev;Username=postgres;Password=postgres",
        "SenderEmail": "dev-sender@yourdomain.com",
        "SenderPassword": "dev-smtp-password",
        "AES_KEY": "dev-aes-key-32-bytes-minimum!!!",
        "ALLOWED_ORIGINS": "https://localhost:3000,https://localhost:5173",
        "SCALAR_BEARER_TOKEN": ""
      }
    }
  }
}
```

### Fail-fast rule (apply to every env var read):

Replace every `?? "hardcoded-fallback"` pattern with a startup validation guard:

```csharp
// ❌ BEFORE — silent fallback hides misconfiguration
var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "hardcoded-key";

// ✅ AFTER — fail loud and fast at startup
var key = Environment.GetEnvironmentVariable("JWT_KEY");
ArgumentException.ThrowIfNullOrWhiteSpace(key, "JWT_KEY environment variable is required.");
```

---

## PHASE 1 — CRITICAL SECURITY FIXES

### Task 1 — Fix Middleware Ordering in `ConfigureApp.cs`

**File:** `projectname.Host/Extensions/ConfigureApp.cs`

**Problem:** `MapEndpoints()` is called before `UserContextMiddleware` and `GlobalExceptionHandlerMiddleware`, so those middlewares never execute for any matched route. `UseCors()` is also registered after auth — too late for preflight handling.

**Required action:** Reorder the pipeline to match this exact sequence:

```csharp
// ✅ CORRECT ORDER
app.UseCors();                                              // 1. Must be first — handles preflight OPTIONS
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();      // 2. Wraps everything — catches all errors
app.UseHttpsRedirection();                                  // 3. Redirect HTTP → HTTPS
app.UseAuthentication();                                    // 4. Validate JWT
app.UseAuthorization();                                     // 5. Evaluate policies
app.UseMiddleware<UserContextMiddleware>();                  // 6. Populate IUserContext from claims
app.UseSerilogRequestLogging();                             // 7. Log after context is populated
app.MapEndpoints();                                         // 8. LAST — terminates the pipeline
```

Remove `app.UseGrpcServices()` from inside `ConfigureApp` if gRPC mapping belongs in `Program.cs` already. Do not add `app.UseGrpcServices()` after `MapEndpoints()`.

---

### Task 2 — Enable JWT Issuer & Audience Validation in `Validator.cs`

**File:** `projectname.Host/Extensions/Validators/Validator.cs`

**Problem:**
```csharp
ValidateIssuer = false,   // TODO will be added in future
ValidateAudience = false, // TODO will be added in future
```

**Required action:** Enable both validations. Read issuer and audience from environment variables (which are now in `launchSettings.json`):

```csharp
var issuer   = Environment.GetEnvironmentVariable("JWT_ISSUER");
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
ArgumentException.ThrowIfNullOrWhiteSpace(issuer,   "JWT_ISSUER environment variable is required.");
ArgumentException.ThrowIfNullOrWhiteSpace(audience, "JWT_AUDIENCE environment variable is required.");

options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,
    ValidIssuer              = issuer,
    ValidateAudience         = true,
    ValidAudience            = audience,
    ValidateLifetime         = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    ClockSkew                = TimeSpan.Zero  // Remove default 5-min tolerance
};
```

---

### Task 3 — Remove Default JWT Key and All Credential Fallbacks

**Files:** `Host/Extensions/Resources.cs`, `DA/DependencyInjection.cs`, `Utility/EmailSender/DependencyInjection.cs`

**Required action:** Apply the STANDING RULE from above to every credential in these files. No `??` fallback is permitted for any secret or connection string. Each env var read must use `ArgumentException.ThrowIfNullOrWhiteSpace()`.

Also update `Resources.cs` CORS setup (see Task 5 below) at the same time since you're already in this file.

---

### Task 4 — Add Startup Configuration Validation in `Program.cs`

**File:** `projectname.Host/Program.cs`

**Required action:** After `var builder = WebApplication.CreateBuilder(args);`, add a centralized startup validation method that verifies all required env vars are present before the app starts:

```csharp
ValidateRequiredEnvironmentVariables();

static void ValidateRequiredEnvironmentVariables()
{
    var required = new[]
    {
        "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE",
        "DBHost", "SenderEmail", "SenderPassword",
        "AES_KEY", "ALLOWED_ORIGINS"
    };

    var missing = required
        .Where(k => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(k)))
        .ToList();

    if (missing.Count > 0)
        throw new InvalidOperationException(
            $"Missing required environment variables: {string.Join(", ", missing)}");
}
```

---

### Task 5 — Restrict CORS to Specific Origins

**Files:** `Host/Extensions/ConfigureApp.cs`, `Host/Extensions/Resources.cs`

**Problem:**
```csharp
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
```

**Required action:** Read allowed origins from the `ALLOWED_ORIGINS` env var (comma-separated) and replace both the DI registration and the pipeline usage:

```csharp
// In Resources.cs DI registration
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
ArgumentException.ThrowIfNullOrEmpty(allowedOrigins.Length == 0 ? null : "ok",
    "ALLOWED_ORIGINS must contain at least one origin.");

services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());  // Only valid with explicit origins, not AllowAnyOrigin
});
```

In `ConfigureApp.cs`, remove the inline `app.UseCors(x => x.AllowAnyOrigin()...)` — the middleware call should simply be `app.UseCors();` using the default policy registered above.

---

### Task 6 — Remove Hardcoded Bearer Token from Scalar / OpenAPI Config

**File:** `Host/Extensions/ConfigureApp.cs`

**Problem:**
```csharp
.AddHttpAuthentication("BearerAuth", auth => {
    auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // ← committed to git
});
```

**Required action:**

Option A (preferred) — Remove the pre-filled token entirely. Let developers paste their own token in the Scalar UI:
```csharp
.AddHttpAuthentication("BearerAuth", auth => {
    auth.Token = Environment.GetEnvironmentVariable("SCALAR_BEARER_TOKEN") ?? string.Empty;
});
```

Option B — If the Scalar config accepts a placeholder, use an empty string. Never commit an actual JWT to source code.

---

### Task 7 — Fix or Remove `CustomAuthHandler` (Always-True Auth)

**File:** `Host/Extensions/Validators/Validator.cs`

**Problem:**
```csharp
private bool ValidateLocalAuth() => true; // backdoor
```

**Required action:** If the `LocalAuthIssuer` scheme is not actually used in any endpoint's `[Authorize]` attribute, **remove the entire `CustomAuthHandler` class and its DI registration**. If it is referenced, replace `return true;` with an actual implementation or throw `NotImplementedException` to make it obvious it is incomplete:

```csharp
private bool ValidateLocalAuth()
    => throw new NotImplementedException(
           "LocalAuth scheme is not yet implemented. Remove [Authorize(AuthenticationSchemes = \"LocalAuthIssuer\")] " +
           "from all endpoints or implement real validation here.");
```

---

### Task 8 — Fix AES to Use Random IV Per Operation

**File:** `Utility/AuthProvider/AESEncryption/CustomAESEncryption.cs`

**Problem:** A single IV is loaded from configuration at startup and reused for every encryption. With AES-CBC, reusing an IV leaks information about plaintext patterns.

**Required action:** Generate a fresh random IV for every encryption call and prepend it to the ciphertext. On decryption, extract the IV from the first 16 bytes:

```csharp
public string Encrypt(string plainText)
{
    using var aes = Aes.Create();
    aes.Key = Convert.FromBase64String(_key); // key from env var, NOT IV
    aes.GenerateIV();                         // fresh random IV every time

    using var encryptor = aes.CreateEncryptor();
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

    // Prepend IV: [16-byte IV][ciphertext]
    var result = new byte[aes.IV.Length + cipherBytes.Length];
    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
    return Convert.ToBase64String(result);
}

public string Decrypt(string cipherTextBase64)
{
    var fullBytes = Convert.FromBase64String(cipherTextBase64);
    var iv        = fullBytes[..16];          // first 16 bytes = IV
    var cipher    = fullBytes[16..];          // rest = actual ciphertext

    using var aes = Aes.Create();
    aes.Key = Convert.FromBase64String(_key);
    aes.IV  = iv;

    using var decryptor = aes.CreateDecryptor();
    var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    return Encoding.UTF8.GetString(plain);
}
```

Remove `AES_IV` from all env vars / `launchSettings.json` — the IV is now generated internally. Keep only `AES_KEY`.

---

### Task 9 — Add Rate Limiting Middleware

**File:** `projectname.Host/Program.cs` (registration) + `ConfigureApp.cs` (pipeline)

**Required action:** Add ASP.NET Core's built-in rate limiting (available since .NET 7):

```csharp
// In Program.cs / Resources.cs DI registration
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit     = 100,
                Window          = TimeSpan.FromMinutes(1),
                QueueLimit      = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
```

```csharp
// In ConfigureApp.cs — add BEFORE MapEndpoints()
app.UseRateLimiter();
```

Add `using System.Threading.RateLimiting;` where needed.

---

### Task 10 — Add Security Headers Middleware

**File:** `projectname.Host/Extensions/ConfigureApp.cs`

**Required action:** Add a middleware call that injects standard security response headers on every response. Place it immediately after `UseCors()`:

```csharp
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]    = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]           = "DENY";
    ctx.Response.Headers["X-XSS-Protection"]          = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"]           = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]        = "camera=(), microphone=(), geolocation=()";
    ctx.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    await next();
});
```

> ✅ Phase 1 Complete Checklist
> - [ ] Middleware order fixed
> - [ ] JWT issuer + audience validation enabled
> - [ ] All credential fallbacks removed; fail-fast guards added
> - [ ] Startup env var validation in Program.cs
> - [ ] CORS restricted to explicit origins from env var
> - [ ] Hardcoded bearer token removed from Scalar config
> - [ ] CustomAuthHandler removed or stubbed as NotImplementedException
> - [ ] AES uses random IV per operation; IV no longer in config
> - [ ] Rate limiting middleware added
> - [ ] Security headers middleware added
> - [ ] launchSettings.json updated with all required env vars

---

## PHASE 2 — ARCHITECTURAL CLEANUP

### Task 11 — Remove All Domain-Specific Code (Oaken / Guard / Barrier)

**Files to modify or delete:**

| File | Action |
|---|---|
| `Core/Features/IFeatures.cs` | Replace entire content with a generic empty `IExample` interface + XML doc comment |
| `DA/Enums/AccessLevelEnum.cs` | **DELETE** |
| `DA/Enums/GuardStatus.cs` | **DELETE** |
| `Utility/Helpers/Common/Constant/KConstant.cs` | Remove `ApiName = "Oaken"`, `BarrierStatusClosedId`, `BarrierStatusOpenId`, `Stagging` (typo) → replace with generic placeholders |
| `projectname.Host/Oaken.Host.http` | **DELETE** |
| `projectname.Host/Commands.txt` | **DELETE** |
| `Dockerfile` | Fix `Oaken.Host/Oaken.Host.csproj` reference → `projectname.Host/projectname.Host.csproj` |

Replacement for `Core/Features/IFeatures.cs`:
```csharp
namespace Core.Features;

/// <summary>
/// Marker interface for feature grouping. Implement a sub-interface per feature domain.
/// Example: IUserFeature, IProductFeature — routes will be grouped under /User, /Product.
/// </summary>
public interface IFeatureGroup { }
```

---

### Task 12 — Fix Dockerfile Template Placeholders

**File:** `projectname.Host/Dockerfile`

**Required action:** Replace every occurrence of `Oaken` with `projectname`. Ensure all `COPY`, `RUN dotnet restore`, and `ENTRYPOINT` lines use the template placeholder. The final Dockerfile should have zero hardcoded project names:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["projectname.Host/projectname.Host.csproj", "projectname.Host/"]
COPY ["Core/Core.csproj",           "Core/"]
COPY ["DA/DA.csproj",               "DA/"]
COPY ["Utility/Utility.csproj",     "Utility/"]
RUN dotnet restore "projectname.Host/projectname.Host.csproj"
COPY . .
WORKDIR "/src/projectname.Host"
RUN dotnet build "projectname.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "projectname.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "projectname.Host.dll"]
```

---

### Task 13 — Delete `Utility/GenericRepository/` Entirely

**Directory:** `Utility/GenericRepository/` (8 files)

**Required action:** Delete the entire directory. The `DA/Persistence/Repository/` is the authoritative repository pattern. Also:
- Remove the `GenericRepository` NuGet references from `Utility.csproj` if they were exclusively used there
- Search for any `using` statements referencing `Utility.GenericRepository` and replace them with the DA layer equivalents
- Fix the typo file: `Utility/GenericRepository/PosgtreSqlStates.cs` is being deleted, so check for any callers first

---

### Task 14 — Remove All Dead / Commented-Out Code

**Files:** Multiple — scan the entire solution

Specifically target:

| Location | What to remove |
|---|---|
| `Utility/NATSNotificationSystem/INatsService.cs` | Entire file is commented out — **DELETE FILE** |
| `Utility/NATSNotificationSystem/NatsService.cs` | Entire implementation commented out — **DELETE FILE** |
| `Utility/NATSNotificationSystem/NatsDummyProgram.cs` | Empty stub — **DELETE FILE** |
| `Utility/NATSNotificationSystem/DependencyInjection.cs` | `AddNatsService` returns immediately — if NATS is not implemented, **DELETE ENTIRE NATS DIRECTORY** |
| `Host/Extensions/Resources.cs` | Remove commented-out `AddSwagger()` and `AddCustomLogger` blocks |
| `Host/Extensions/ConfigureApp.cs` | Remove commented-out migration code |
| `comment.txt` (root) | **DELETE** |
| `projectname.Host/Endpoints.cs` | If unused/unclear — **DELETE or stub with a TODO comment** |

After deletions, verify the solution still builds: `dotnet build`.

---

### Task 15 — Deduplicate `KAuthClaimTypes`

**Files:**
1. `Utility/Helpers/Auth/HTTPContextUserRetriever.cs` — nested `KAuthClaimTypes` class
2. `Utility/Helpers/Common/Auth/KAuthClaimTypes.cs` — standalone class

**Required action:**
- Keep the **standalone** `Utility/Helpers/Common/Auth/KAuthClaimTypes.cs` as the single source of truth
- Remove the nested `KAuthClaimTypes` class from `HTTPContextUserRetriever.cs`
- Update all `using` or internal references in `HTTPContextUserRetriever.cs` to point to the standalone class
- Run a solution-wide search for `KAuthClaimTypes` to ensure no stale references remain

---

### Task 16 — Begin Moving Infrastructure Concerns from Utility to Infrastructure/

**Target directory:** `Infrastructure/` (currently empty)

**Required action:** Create the directory structure and begin migrating:

```
Infrastructure/
  Redis/
    RedisSessionManager.cs          ← moved from Utility/Redis/
    DependencyInjection.cs          ← new, extracted from Utility DI
  Email/
    EmailService.cs                 ← moved from Utility/EmailSender/
    DependencyInjection.cs
  S3/
    S3Helper.cs                     ← moved from Utility/Helpers/S3Helper/
    DependencyInjection.cs
  Slack/
    SlackExceptionMethods.cs        ← moved from Utility/Logger/
  Infrastructure.csproj             ← new project file
```

Create `Infrastructure.csproj` referencing only the NuGet packages needed by these concerns:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StackExchange.Redis"       Version="*" />
    <PackageReference Include="MailKit"                   Version="*" />
    <PackageReference Include="AWSSDK.S3"                 Version="*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Utility\Utility.csproj" />
  </ItemGroup>
</Project>
```

Add `Infrastructure.csproj` to the solution: `dotnet sln add Infrastructure/Infrastructure.csproj`
Add project reference in Host: `<ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />`

> Note: Full Utility split (Task 17) is a large effort. In this session, focus on getting the Infrastructure project scaffolded and the above four concerns migrated.

---

### Task 17 — Begin Splitting Utility into Focused Projects

**This is a Large task — complete what you can, leave a TODO for the rest.**

Start by splitting out `Auth` as it contains security-critical code:

```
Auth/
  JWT/
    JWT.cs                          ← from Utility/Helpers/Auth/JWT/
  TOTP/
    TotpService.cs                  ← from Utility/AuthProvider/
  AES/
    CustomAESEncryption.cs          ← from Utility/AuthProvider/AESEncryption/
  Authorization/
    CustomAuthHandler.cs            ← (or delete if Task 7 removed it)
  Auth.csproj                       ← new project
```

`Auth.csproj` references:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt"               Version="*" />
```

If time is limited, create the project scaffold with empty placeholder files and add a `// TODO: migrate from Utility` comment. The important thing is the project exists and is wired into the solution.

---

### Task 18 — Fix `UserPayload` Required Properties

**File:** `Utility/Helpers/Auth/Models/UserPayload.cs`

**Problem:** `required string` properties throw at object creation time if JWT claims are missing — no graceful handling.

**Required action:** Change `required string` to nullable `string?` for all JWT-derived claims, and add null-safe accessors:

```csharp
public class UserPayload
{
    public string? UserId    { get; set; }
    public string? Email     { get; set; }
    public string? UserType  { get; set; }
    public string? Resources { get; set; }
    public string? SessionStartDate { get; set; }
    public string? SessionEndDate   { get; set; }

    /// <summary>Returns true if the minimum required claims are present.</summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(UserId) &&
        !string.IsNullOrWhiteSpace(UserType);
}
```

Update `UserContextMiddleware` to call `IsValid()` and return `401` if the payload is incomplete rather than silently proceeding with null values.

---

### Task 19 — Replace `System.Net.Mail` with MailKit in `EmailService.cs`

**File:** `Utility/EmailSender/EmailService.cs` (or new location in `Infrastructure/Email/` if Task 16 is done)

**Problem:** `System.Net.Mail.SmtpClient` is deprecated by Microsoft. MailKit is already a project dependency.

**Required action:** Rewrite using MailKit:

```csharp
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int    _port;
    private readonly string _senderEmail;
    private readonly string _senderPassword;

    public EmailService(IConfiguration config)
    {
        _host          = config["SMTP_HOST"]     ?? "localhost";
        _port          = int.Parse(config["SMTP_PORT"] ?? "587");
        _senderEmail   = Environment.GetEnvironmentVariable("SenderEmail")
                         ?? throw new InvalidOperationException("SenderEmail is required.");
        _senderPassword = Environment.GetEnvironmentVariable("SenderPassword")
                         ?? throw new InvalidOperationException("SenderPassword is required.");
    }

    public async Task SendAsync(string to, string subject, string htmlBody,
                                CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_senderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_senderEmail, _senderPassword, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
```

Add `SMTP_HOST` and `SMTP_PORT` to `launchSettings.json` as well.

---

### Task 20 — Fix `SlackExceptionMethods` (async void + HttpClient)

**File:** `Utility/Logger/SlackExceptionMethods.cs` (or new `Infrastructure/Slack/` location)

**Problem 1:** `async void` means unobserved exceptions crash the process.
**Problem 2:** `new HttpClient()` per call causes socket exhaustion.

**Required action:**

```csharp
public class SlackExceptionLogger
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackExceptionLogger> _logger;
    private readonly string _webhookUrl;

    public SlackExceptionLogger(IHttpClientFactory factory,
                                ILogger<SlackExceptionLogger> logger,
                                IConfiguration config)
    {
        _httpClientFactory = factory;
        _logger            = logger;
        _webhookUrl        = config["SLACK_WEBHOOK_URL"] ?? string.Empty;
    }

    // ✅ Returns Task — caller can await or fire-and-forget safely
    public async Task LogExceptionAsync(Exception ex, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl)) return;

        try
        {
            var client  = _httpClientFactory.CreateClient("slack");
            var payload = JsonSerializer.Serialize(new { text = ex.ToString() });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await client.PostAsync(_webhookUrl, content, ct);
        }
        catch (Exception inner)
        {
            // Log but never rethrow — Slack failure must not break request handling
            _logger.LogWarning(inner, "Failed to send exception to Slack.");
        }
    }
}
```

Register with `IHttpClientFactory` in DI:
```csharp
services.AddHttpClient("slack");
services.AddSingleton<SlackExceptionLogger>();
```

Add `SLACK_WEBHOOK_URL` to `launchSettings.json` with an empty default.

> ✅ Phase 2 Complete Checklist
> - [ ] All Oaken/Guard/Barrier domain code removed
> - [ ] Dockerfile template placeholders fixed
> - [ ] `Utility/GenericRepository/` deleted
> - [ ] Dead/commented-out code removed across solution
> - [ ] `KAuthClaimTypes` deduplicated to one location
> - [ ] `Infrastructure/` project created and scaffolded
> - [ ] `Auth/` project scaffold created
> - [ ] `UserPayload` required → nullable with `IsValid()` guard
> - [ ] `EmailService` migrated to MailKit
> - [ ] `SlackExceptionMethods` fixed (Task + IHttpClientFactory)
> - [ ] Solution builds cleanly after all changes: `dotnet build`

---

## EXECUTION RULES FOR COPILOT

1. **Work one task at a time.** Complete, verify, then move to the next.
2. **Run `dotnet build` after every task.** Fix any compilation errors before proceeding.
3. **Apply the STANDING RULE** (credentials to launchSettings) at the start of every task that touches a file listed in the credential table.
4. **Never introduce new hardcoded secrets.** Every secret must come from an environment variable.
5. **Do not modify files not listed in a task** unless they contain a credential that must be moved.
6. **Do not start Phase 3 tasks** (test projects, Docker Compose, CI/CD, OTEL, versioning, Polly, migrations, Kubernetes). These are out of scope for this session.
7. **When deleting files**, check for references first: `grep -r "FileName" --include="*.cs" .`
8. **Preserve well-designed files.** Do not refactor `ExampleFeatures.cs`, `RegisterFeatures.cs`, `UnitOfWork.cs`, `AppDbContext.cs`, `BaseContext.cs`, `GlobalExceptionHandlerMiddleware.cs`, or `OpenTelemetryExtensions.cs` unless a specific task requires it.

---

*Generated from architecture review of `template-svc` — .NET 10 ASP.NET Core Minimal API*
*Phase 3 (Enterprise Capabilities) is excluded from this prompt — handle in a separate session.*

