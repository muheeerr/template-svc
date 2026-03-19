# Tahaq — Enterprise Architecture Analysis
### Senior Architect Review · .NET 10 · ASP.NET Minimal API · WolverineFx · RabbitMQ · EF Core 10

> **Purpose:** This document is a comprehensive architectural audit of the Tahaq codebase from the perspective of a senior architect preparing it for enterprise-level production. It covers the current design, identified flaws, security vulnerabilities, and a concrete roadmap to production-readiness.

---

## Table of Contents

1. [Current Architecture Overview](#1-current-architecture-overview)
2. [Component & Layer Diagram](#2-component--layer-diagram)
3. [Request Lifecycle Flow](#3-request-lifecycle-flow)
4. [SAP Integration Event Flow](#4-sap-integration-event-flow)
5. [Data Layer Architecture](#5-data-layer-architecture)
6. [Authentication & Authorization Flow](#6-authentication--authorization-flow)
7. [🔴 Critical Security Vulnerabilities](#7--critical-security-vulnerabilities)
8. [🟠 High-Severity Architecture Flaws](#8--high-severity-architecture-flaws)
9. [🟡 Medium-Severity Issues](#9--medium-severity-issues)
10. [🟢 Low-Severity / Design Observations](#10--low-severity--design-observations)
11. [✅ What Works Well](#11--what-works-well)
12. [Enterprise-Level Roadmap](#12-enterprise-level-roadmap)
13. [Target Enterprise Architecture](#13-target-enterprise-architecture)
14. [Technology Stack Assessment](#14-technology-stack-assessment)

---

## 1. Current Architecture Overview

Tahaq is a **SAP integration middleware** built on ASP.NET Core 10 Minimal APIs. It acts as an event-driven bridge between SAP ERP systems and internal consumers — receiving events from SAP via RabbitMQ, enriching them by calling a SAP Middleware REST API, persisting data to PostgreSQL, and publishing outbound events.

### Solution Structure

```
Tahaq.slnx
├── Tahaq.Host/         ← Entry point, DI composition, middleware pipeline, WolverineFx config
│   ├── Program.cs
│   ├── Extensions/
│   │   ├── Resources.cs          ← Master DI registration
│   │   ├── ConfigureApp.cs       ← Middleware pipeline builder
│   │   ├── WolverineExtensions.cs← RabbitMQ transport wiring
│   │   └── OpenTelemetryExtensions.cs
│   ├── Middlewares/
│   │   ├── GlobalExceptionHandlerMiddleware.cs
│   │   └── UserContextMiddleware.cs
│   ├── appsettings.json          ← ⚠️ Contains live credentials
│   └── Dockerfile
│
├── Core/               ← Business logic, features, SAP integration
│   ├── Features/       ← HTTP endpoint classes (features-as-classes pattern)
│   │   ├── Item/       ← CRUD for items
│   │   ├── ItemInspectionCard/
│   │   ├── ItemSample/
│   │   └── ...
│   ├── SapIntegration/
│   │   ├── Commands/   ← Mediator commands (ProcessSapEventCommand)
│   │   ├── Config/     ← SapIntegrationOptions
│   │   ├── ExternalApi/← Refit HTTP clients
│   │   ├── Handlers/   ← Wolverine message handlers
│   │   ├── Jobs/       ← BackgroundService scheduler
│   │   └── Messages/   ← Event record types
│   ├── Endpoints/      ← Assembly scanner + route mapper
│   └── DependencyInjection.cs
│
├── DA/                 ← Data access: EF Core, entities, UoW, repository
│   ├── Entities/       ← Domain entities inheriting Entity base
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── BaseContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Repository/
│   │   └── Configurations/
│   └── DependencyInjection.cs
│
└── Utility/            ← Reusable cross-cutting concerns
    ├── AuthProvider/   ← AES, TOTP
    ├── CustomHTTP/     ← HTTP status code constants
    ├── EmailSender/
    ├── EndpointController/   ← IFeature interface
    ├── EndpointExposerGRPC/  ← gRPC exposure
    ├── GenericRepository/
    ├── Helpers/        ← JWT, auth models, filters, response helpers
    ├── Logger/
    ├── NATSNotificationSystem/ ← ⚠️ Dead code (commented out)
    ├── SessionManager/ ← ⚠️ Redis commented out
    └── Protos/         ← gRPC proto definitions
```

---

## 2. Component & Layer Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        EXTERNAL SYSTEMS                                   │
│                                                                            │
│  ┌──────────────┐    ┌────────────────────┐    ┌────────────────────┐    │
│  │  SAP ERP     │    │  SAP Middleware API│    │  API Consumers     │    │
│  │  (Producer)  │    │  163.61.91.180:8070│    │  (Frontend/Others) │    │
│  └──────┬───────┘    └─────────┬──────────┘    └─────────┬──────────┘    │
└─────────│───────────────────────│────────────────────────│────────────────┘
          │ RabbitMQ               │ HTTP/Refit             │ HTTP REST
          ▼                        │                        ▼
┌──────────────────────────────────│─────────────────────────────────────────┐
│                          TAHAQ HOST                                        │
│                                  │                                         │
│  ┌────────────────────────────────│────────────────────────────────────┐   │
│  │              MIDDLEWARE PIPELINE                                     │   │
│  │  HttpsRedirection → [Auth] → UserContext → GlobalExceptionHandler   │   │
│  │  ⚠️ ORDER WRONG - ExceptionHandler is too late in pipeline           │   │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                  │                                         │
│  ┌────────────────────────────────│────────────────────────────────────┐   │
│  │            WolverineFx (Message Bus)                                 │   │
│  │  RabbitMQ Transport ←──────────┘                                    │   │
│  │  ┌──────────────────┐   ┌──────────────────┐                        │   │
│  │  │SapDataChanged    │   │SapItemCreated/   │                        │   │
│  │  │Handler           │   │Updated Handler   │                        │   │
│  │  └────────┬─────────┘   └────────┬─────────┘                       │   │
│  │           │ Mediator              │ Refit                           │   │
│  └───────────│───────────────────────│─────────────────────────────────┘  │
│              ▼                       ▼                                     │
│  ┌───────────────────────────────────────────────────────────────────┐    │
│  │                    CORE LAYER                                      │    │
│  │  Features (HTTP Endpoints)  │  SAP Integration Commands/Handlers   │    │
│  │  ┌──────────┐ ┌──────────┐  │  ┌───────────────────────────────┐  │    │
│  │  │AddItem   │ │GetItems  │  │  │ProcessSapEventHandler (Mediator│  │    │
│  │  │UpdateItem│ │DeleteItem│  │  │+ ExternalApiClient + Wolverine)│  │    │
│  │  └────┬─────┘ └────┬─────┘  │  └──────────────┬────────────────┘  │    │
│  └───────│────────────│─────────│─────────────────│────────────────────┘   │
│          │            │         │                  │                        │
│  ┌───────▼────────────▼─────────▼──────────────────▼────────────────┐    │
│  │                    DATA ACCESS LAYER (DA)                          │    │
│  │  IUnitOfWork → Repository<TEntity> → AppDbContext → PostgreSQL     │    │
│  │  + WolverineFx Inbox/Outbox (wolverine schema in PostgreSQL)       │    │
│  └───────────────────────────────────────────────────────────────────┘    │
│                                                                            │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │                    OBSERVABILITY                                    │   │
│  │  Serilog (Console + Seq)  │  OpenTelemetry (Traces + Metrics)      │   │
│  └────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│        INFRASTRUCTURE           │
│  PostgreSQL (App + Wolverine)   │
│  RabbitMQ (163.61.91.156:30672) │
│  Redis (commented out)          │
│  NATS (commented out)           │
└─────────────────────────────────┘
```

---

## 3. Request Lifecycle Flow

```
Client HTTP Request
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│                  MIDDLEWARE PIPELINE                        │
│                                                             │
│  1. UseHttpsRedirection                                     │
│  2. UseAuthentication    ← skipped entirely in Development! │
│  3. UseAuthorization     ← skipped entirely in Development! │
│  4. MapEndpoints() — routes registered here                 │
│  5. UseMiddleware<UserContextMiddleware>                     │
│  6. UseMiddleware<GlobalExceptionHandlerMiddleware> ⚠️ late  │
│  7. UseCors ⚠️ double CORS (also in Resources.cs)            │
└───────────────────────────────────────────────────────────┘
        │ route matched
        ▼
┌───────────────────────────────────────────────────────────┐
│               ENDPOINT EXECUTION                           │
│                                                             │
│  IFeature.Map() → RouteGroup                               │
│       │                                                     │
│       ├── WithRequestValidation<TRequest>()                 │
│       │        │                                            │
│       │        ▼                                            │
│       │   RequestValidationFilter<T>                        │
│       │        │ invalid → returns HTTP 402 ⚠️ (wrong code) │
│       │        │ valid   ──────────────────────────────┐   │
│       │                                                 │   │
│       └── Handle(dto, IUnitOfWork, CancellationToken) ◄─┘  │
│                │                                            │
│                ├── Business logic                           │
│                ├── db.Repository.Query()                    │
│                ├── db.CommitAsync()                         │
│                └── ApiResponseHelper.Success/Failure()      │
└───────────────────────────────────────────────────────────┘
        │
        ▼
   HTTP Response (wrapped in ApiResponseModel)
```

---

## 4. SAP Integration Event Flow

### Inbound (SAP → Tahaq)

```
SAP ERP
  │
  │ Publishes JSON message to RabbitMQ
  ▼
RabbitMQ Exchange/Queue
  │
  │  Three separate paths:
  │
  ├─── Queue: sap-inbound (direct)
  │         │
  │         ▼
  │   WolverineFx (inbox durability via PostgreSQL)
  │         │
  │         ▼
  │   SapDataChangedHandler.Handle(SapDataChangedEvent)
  │         │
  │         │ Mediator.Send(ProcessSapEventCommand)  ← ⚠️ double dispatch
  │         ▼
  │   ProcessSapEventHandler
  │         │
  │         ├── ExternalApiClient.GetEntityDetailAsync()  ← HTTP call to external API
  │         ├── [TODO: persist to DB]
  │         └── messageBus.PublishAsync(SapOutboundEvent)
  │                   │
  │                   ▼
  │             Queue: sap-outbound → SAP or consumers
  │
  ├─── Exchange: item-created-exchange (fanout) → Queue: item-created-tahaq-queue
  │         │
  │         ▼
  │   SapItemHandler.Handle(SapItemCreatedEvent)
  │         │
  │         ├── middlewareApi.GetItemByCodeAsync()  ← HTTP call to SAP Middleware
  │         ├── unitOfWork.Items.GetFirstOrDefaultWithInclude()
  │         │       ⚠️ Race condition: two concurrent messages for same ItemCode
  │         ├── unitOfWork.Items.AddAsync() OR UpdateAsync()
  │         └── unitOfWork.CommitAsync()
  │
  └─── Exchange: item-updated-exchange (fanout) → Queue: item-updated-tahaq-queue
            │
            ▼
      SapItemHandler.Handle(SapItemUpdatedEvent)  ← same UpsertAsync()
```

### Background Publisher Job (Periodic)

```
SendSapDataChangedEventJob (BackgroundService)
  │
  │ Every N seconds (default 300s from config)
  │
  ▼
bus.PublishAsync(SapItemCreatedEvent("RM000015"))  ⚠️ hardcoded item code!
  │
  └── Task.Delay(2000)
  │
  └── bus.PublishAsync(SapItemUpdatedEvent("RM000015"))
            │
            ▼
      → item-created-exchange / item-updated-exchange (fanout)
            │
            ▼
      (Loops back into SapItemHandler above)
```

---

## 5. Data Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     IUnitOfWork                                  │
│                                                                  │
│  Repositories:                                                   │
│  ├── IRepository<UnitOfMeasure, AppDbContext>                   │
│  ├── IRepository<LubricantAttribute, AppDbContext>              │
│  ├── IRepository<Item, AppDbContext>                            │
│  ├── IRepository<ItemInspectionCard, AppDbContext>              │
│  ├── IRepository<ItemSample, AppDbContext>                      │
│  └── ... (8 more repositories)                                  │
│                                                                  │
│  Transaction Management:                                         │
│  ├── BeginTransactionAsync()                                    │
│  ├── CommitTransactionAsync()                                   │
│  ├── RollbackTransactionAsync()                                 │
│  └── CommitAsync() / Commit() [sync ⚠️]                          │
└──────────────────────┬──────────────────────────────────────────┘
                       │ lazy-init via ConcurrentDictionary
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│               Repository<TEntity, TContext>                      │
│                                                                  │
│  30+ methods including:                                          │
│  ├── GetFirstOrDefaultWithInclude()                             │
│  ├── GetWithInclude<TResult>() (with projection + pagination)   │
│  ├── GetAllWithPagination()                                     │
│  ├── GetPaginationWithIncludeAsync()                            │
│  ├── GetManyIQueryable() [exposes IQueryable ⚠️ leaks abstraction]│
│  ├── ToDictionaryAsync()                                        │
│  ├── DeleteAsync() / DeleteWithIdsAsync()                       │
│  └── AddRangeAsync()                                            │
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                  AppDbContext : BaseContext                       │
│                                                                  │
│  Global Query Filters (⚠️ CRITICAL BUG: second filter replaces   │
│  first in EF Core — IsDeleted filter IS SILENTLY DROPPED):       │
│  ├── HasQueryFilter(BuildIsDeletedFilter)   ← overwritten!       │
│  └── HasQueryFilter(BuildIsActiveFilter)    ← only this applies  │
│                                                                  │
│  Auditing via Reflection (⚠️ slow, fragile):                     │
│  ├── CreatedAt / CreatedBy (⚠️ always overwrites even if set)    │
│  └── UpdatedAt / UpdatedBy                                       │
│                                                                  │
│  BaseContext.GetUserName():                                       │
│  └── Service-locates IHttpContextAccessor ⚠️ DA depends on HTTP  │
└─────────────────────────────────────────────────────────────────┘

Entity Base Hierarchy:
  Entity : BaseAuditableEntity
    ├── Id : Guid (UUIDv7, default generated in PostgreSQL via uuidv7())
    ├── CreatedAt : DateTimeOffset
    ├── CreatedBy : string (nullable!)
    ├── UpdatedAt : DateTimeOffset?
    ├── UpdatedBy : string?
    ├── IsActive : bool (default true)
    └── IsDeleted : bool (default false)
```

---

## 6. Authentication & Authorization Flow

```
JWT Configuration (from Environment Variables):
  JWT_KEY              (default: "asdavvasd132132131231232312312dsadasdsdsdsds@asd112" ⚠️ weak default)
  JWT_ISSUER           (default: "localhost")
  JWT_AUDIENCE         (default: "localhost")
  JWT_ACCESS_TOKEN_EXPIRATION_IN_MINUTES (default: 10)
  JWT_REFRESH_TOKEN_EXPIRATION_IN_DAYS   (default: 10)

Token Generation (Jwt.GenerateToken):
  UserPayload → ClaimsIdentity → SecurityTokenDescriptor
    ⚠️ Issuer and Audience are NOT set in the descriptor
    ⚠️ Token has no Issuer/Audience claims despite being configured
  → HmacSha256 signed JWT
  + Random 32-byte refresh token (not stored anywhere ⚠️)

Authentication Check (ConfigureApp):
  if (Environment == "Development")
    UseAuthentication() SKIPPED  ⚠️
    UseAuthorization()  SKIPPED  ⚠️
  else
    UseAuthentication()
    UseAuthorization()

Authorization Policy (KPolicyDescriptor.CustomPolicy):
  if (Development) → RequireAssertion(_ => true)  ← always passes ⚠️
  else → RequireAuthenticatedUser() + CustomAuthorizationRequirement

CustomAuthorizationHandler:
  (not shown — behavior unknown from code seen)

UserContextMiddleware:
  ├── Extracts UserPayload from JWT claims
  ├── SessionStartDate check: if future → return 403 ⚠️ (should be 401)
  └── Injects into scoped IUserContext
```

---

## 7. 🔴 Critical Security Vulnerabilities

These are production-blocking issues that expose the system to immediate compromise.

---

### 7.1 — Hardcoded Live Credentials in Source Code

**Files:** `appsettings.json`, `SapIntegrationOptions.cs`

```json
// appsettings.json — LIVE PRODUCTION CREDENTIALS IN VERSION CONTROL
"SapIntegration": {
    "RabbitMq": {
        "Host": "163.61.91.156",
        "Port": 30672,
        "UserName": "admin",
        "Password": "admin123"   // ← LIVE PASSWORD IN GIT
    }
}
```

```csharp
// SapIntegrationOptions.cs — hardcoded defaults with live values
public string BaseUrl { get; init; } = "http://163.61.91.180:8070";
public string ApiKey { get; init; } = "super";  // ← API KEY IN SOURCE
```

```csharp
// DA/DependencyInjection.cs — DB password as fallback default
var DBhost = Environment.GetEnvironmentVariable("DBHost")
    ?? "Host=localhost;Port=5433;Database=TahaqDb;Username=admin;Password=admin123";
```

**Impact:** Anyone with repository access can directly connect to the live RabbitMQ broker, middleware API, and database. This is a P0/Sev-1 breach if this repository is public or shared.

**Fix:** Rotate all credentials immediately. Use `dotnet user-secrets` for development, environment variables (never file) for production, or a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).

---

### 7.2 — Authentication Completely Disabled in Development

**File:** `ConfigureApp.cs`, `Resources.cs`

```csharp
// Authentication and authorization middleware is entirely SKIPPED
if (!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
}
```

```csharp
// The policy itself is a no-op in Development
if (isDevelopment)
    options.AddPolicy(CustomPolicy, policy => policy.RequireAssertion(_ => true));
```

**Impact:** A misconfigured `ASPNETCORE_ENVIRONMENT=Development` in a staging/production environment exposes ALL endpoints without authentication. Defense-in-depth requires auth to run everywhere; disable enforcement at the *policy* level if needed for testing, not at the middleware level.

**Fix:** Always register authentication/authorization middleware. Use an "allow-all" policy only for specific development routes, not globally.

---

### 7.3 — Wildcard CORS on Both Layers (Double Registration)

**Files:** `Resources.cs`, `ConfigureApp.cs`

```csharp
// Resources.cs — registered once
.AddCors(options => options.AddDefaultPolicy(
    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ConfigureApp.cs — registered AGAIN with inline lambda
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
```

**Impact:** CORS is configured twice (AddCors default policy and inline UseCors policy). The wildcard CORS allows ANY origin — this opens the API to CSRF and cross-origin data theft in browser clients.

**Fix:** Register CORS once. For enterprise use, specify allowed origins explicitly per environment. Never `AllowAnyOrigin` + `AllowCredentials` together (this is also a framework error).

---

### 7.4 — JWT Secret Has Weak Default & No Minimum Length Validation

**File:** `Resources.cs`

```csharp
var Key = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "asdavvasd132132131231232312312dsadasdsdsdsds@asd112";
```

**Impact:** If `JWT_KEY` is not set in production, the application silently uses a known default key — anyone who reads this source can forge valid JWTs.

**Fix:** Throw at startup if `JWT_KEY` is null/empty. Enforce minimum 32-character keys. Use a dedicated key management service.

---

### 7.5 — Refresh Token is Generated but Never Stored

**File:** `JWT.cs`

```csharp
var refreshToken = GenerateRefreshToken(); // 32 cryptographic bytes
// ... returned to client
// ⚠️ Never persisted anywhere. Cannot be validated or revoked.
```

**Impact:** Refresh tokens cannot be validated on reuse, cannot be revoked (no logout mechanism), and are vulnerable to replay attacks indefinitely.

---

### 7.6 — Hardcoded JWT Token Fragment in Scalar UI

**File:** `ConfigureApp.cs`

```csharp
auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
```

Even truncated, embedding a token in source code is bad practice and may expose partial authentication material.

---

## 8. 🟠 High-Severity Architecture Flaws

---

### 8.1 — EF Core Query Filters: IsDeleted Filter Is Silently Dropped

**File:** `AppDbContext.cs` — **Critical Data Integrity Bug**

```csharp
var isDeletedProperty = entityType.FindProperty(nameof(Entity.IsDeleted));
if (isDeletedProperty != null)
    entity.HasQueryFilter(BuildIsDeletedFilter(entityType.ClrType));  // ← SET

var isActiveProperty = entityType.FindProperty(nameof(Entity.IsActive));
if (isActiveProperty != null)
    entity.HasQueryFilter(BuildIsActiveFilter(entityType.ClrType));  // ← REPLACES the first!
```

**Impact:** EF Core's `HasQueryFilter` **replaces** the existing filter, it does **not** compose them. The `IsDeleted` filter is silently overwritten by the `IsActive` filter. This means **soft-deleted entities are returned** in all queries as long as `IsActive = true`. Data that should be hidden is visible.

**Fix:** Combine both conditions into a single filter expression:

```csharp
entity.HasQueryFilter(BuildCombinedFilter(entityType.ClrType));

private static LambdaExpression BuildCombinedFilter(Type entityType)
{
    var param = Expression.Parameter(entityType, "e");
    var notDeleted = Expression.Equal(
        Expression.Property(param, nameof(Entity.IsDeleted)),
        Expression.Constant(false));
    var isActive = Expression.Equal(
        Expression.Property(param, nameof(Entity.IsActive)),
        Expression.Constant(true));
    return Expression.Lambda(Expression.AndAlso(notDeleted, isActive), param);
}
```

---

### 8.2 — Middleware Pipeline Order is Wrong

**File:** `ConfigureApp.cs`

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();  // only non-dev
app.UseAuthorization();   // only non-dev
app.MapEndpoints();       // ← routes registered here

app.UseMiddleware<UserContextMiddleware>();          // ← AFTER routes
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // ← AFTER routes ⚠️
app.UseCors(...);                                   // ← AFTER routes ⚠️
```

**Impact:**
- `GlobalExceptionHandlerMiddleware` must be the **first** middleware to wrap the entire pipeline. Registered late, it may miss exceptions thrown by routing or authentication.
- `UseCors` registered after endpoints means CORS headers may not be applied for some routes.
- `UserContextMiddleware` after routes means `IUserContext` may not be populated when endpoints execute.

**Correct Order:**
```csharp
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // FIRST — wraps everything
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserContextMiddleware>();
app.MapEndpoints();       // LAST — terminal routing
```

---

### 8.3 — Upsert Race Condition (No Distributed Locking)

**File:** `SapItemHandler.cs`

```csharp
var existing = await unitOfWork.Items.GetFirstOrDefaultWithInclude(
    x => x.ItemCode == itemCode, ct: cancellationToken);

if (existing is not null)
    await unitOfWork.Items.UpdateAsync(existing, ...);
else
    await unitOfWork.Items.AddAsync(item, ...);  // ← Two concurrent messages both reach here
                                                   //   → PostgreSQL unique constraint violation
```

**Impact:** Two simultaneous `SapItemCreatedEvent` messages for the same `ItemCode` will both find no existing record and both try to insert — causing a `UniqueConstraintException` from PostgreSQL, triggering Wolverine retry, and consuming retry budget.

**Fix:** Use PostgreSQL's `ON CONFLICT DO UPDATE` (upsert), a distributed lock (Redis `SETNX`), or a serialized processing queue per item code.

---

### 8.4 — Auditing: `CreatedBy` is Always Overwritten

**File:** `AppDbContext.cs`

```csharp
private void SetIfNullOrEmpty(Type type, object entity, string propertyName, string value)
{
    var prop = type.GetProperty(propertyName);
    if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
    {
        var currentValue = prop.GetValue(entity) as string;
        // ⚠️ This condition is COMMENTED OUT:
        // if (string.IsNullOrWhiteSpace(currentValue))
        {
            prop.SetValue(entity, value);  // ALWAYS overwrites!
        }
    }
}
```

**Impact:** `SapItemHandler` explicitly sets `CreatedBy = "SAP"` on new entities. The auditing code then overwrites this with the current HTTP user (or "SYSTEM"), destroying the original source information.

---

### 8.5 — Double In-Process Message Dispatch (Wolverine + Mediator)

**Files:** `SapDataChangedHandler.cs` → `ProcessSapEventCommand.cs`

```
RabbitMQ → Wolverine → SapDataChangedHandler → Mediator → ProcessSapEventHandler
```

This path uses two separate in-process message dispatchers for a single event. Wolverine **already provides** in-process command dispatch via `IMessageBus.InvokeAsync<T>()`. Adding Mediator creates:

- Unnecessary indirection and debugging complexity
- Two IoC scopes per message
- Two separate handler discovery mechanisms to maintain

**Fix:** Either use Wolverine as the sole dispatcher, or use Mediator without Wolverine's in-process dispatch. Don't use both for the same message path.

---

### 8.6 — Auto-Migration on Startup in Production

**File:** `ConfigureApp.cs`

```csharp
await dbContext.Database.MigrateAsync();
```

**Impact:** Running database migrations automatically on application startup in a production/clustered environment is dangerous:
- In a Kubernetes deployment with 3 replicas, all 3 pods apply migrations simultaneously
- A failed migration can leave the database in a partial state
- No ability to test migrations independently before rollout
- Zero downtime deployments require migration-first strategies (expand/contract pattern)

**Fix:** Migrations should be an explicit, separate step in your CI/CD pipeline, not an application concern.

---

### 8.7 — Background Job Contains Test/Debug Code in Production

**File:** `SendSapDataChangedEventJob.cs`

```csharp
// Hardcoded test data published every N seconds in production:
var itemCreated = new SapItemCreatedEvent("RM000015", Guid.CreateVersion7().ToString());
var itemUpdated = new SapItemUpdatedEvent("RM000015", Guid.CreateVersion7().ToString());

await bus.PublishAsync(itemCreated);
await Task.Delay(2000);
await bus.PublishAsync(itemUpdated);
```

**Impact:** This code continuously publishes fake events to the real message bus, triggering real database upserts for a hardcoded item code. This is test scaffolding that was committed to production code. The original `sapEvent` is even commented out, replaced by this test path.

---

### 8.8 — DA Layer Depends on HTTP Context (Layer Violation)

**File:** `BaseContext.cs`

```csharp
public string GetUserName()
{
    var httpContext = this.GetService<IHttpContextAccessor>()?.HttpContext;
    ...
}
```

**Impact:** The Data Access Layer (DA) uses service locator pattern to reach into the HTTP request context. This means:
- DA assembly depends on ASP.NET Core HTTP abstractions
- Auditing is impossible in non-HTTP contexts (background jobs, Wolverine handlers)
- Testing requires mocking HTTP context

When a Wolverine handler (not HTTP) calls `CommitAsync()`, `GetUserName()` returns `"SYSTEM"` because there's no HTTP context — this is the correct behavior, but the mechanism is fragile and architecturally wrong.

**Fix:** Pass `ICurrentUserService` into the DbContext constructor or use an ambient context pattern. The service should be set by both HTTP middleware and Wolverine middleware.

---

## 9. 🟡 Medium-Severity Issues

---

### 9.1 — Validation Returns HTTP 402 (Payment Required)

**File:** `RequestValidationFilter.cs`

```csharp
return ApiResponseHelper.Convert(true, false, "Validation failed", 402, null, ...);
```

HTTP 402 means "Payment Required." Validation failures should return **422 Unprocessable Entity** or **400 Bad Request**. This will confuse API consumers and break any client that interprets HTTP status codes correctly.

---

### 9.2 — Repository Interface Violates Interface Segregation Principle (ISP)

**File:** `IRepository.cs`

The interface has **30+ methods**, including:
- Multiple overloads of `GetWithInclude`, `GetFirstOrDefault`, `GetLastOrDefault`
- `IQueryable<T>` exposure (leaks EF abstraction)
- Both paginated and non-paginated variants of every query
- Both synchronous and asynchronous variants

**Impact:** Impossible to mock meaningfully in unit tests. Every test must stub 30+ methods. Violates SOLID principles.

**Fix:** Create focused query interfaces: `IReadRepository<T>`, `IWriteRepository<T>`, and separate query objects for complex reads.

---

### 9.3 — Inconsistent Named Parameter Conventions

**File:** `IRepository.cs` and usage across `Core/`

```csharp
// Some methods use "ct", others use "cancellation", none use "cancellationToken":
GetFirstOrDefaultWithInclude(predicate, includeDeleted, ct: token, include);
AddAsync(entity, createNewId, cancellation: token);
GetAllAsync(cancellation: token);
```

This inconsistency makes the API error-prone and unpleasant for developers. Named parameters should be consistent with .NET conventions (`cancellationToken`).

---

### 9.4 — `IQueryable` Exposed from Repository

```csharp
IQueryable<TEntity> GetManyIQueryable(Expression<Func<TEntity, bool>> where);
IQueryable<TEntity> GetManyIQueryableWithDeleted(Expression<Func<TEntity, bool>> where);
```

**Impact:** Exposing `IQueryable` from the DA layer allows callers to compose arbitrary EF queries outside the repository, bypassing global filters (including the already-broken `IsDeleted` filter), auditing, and any future DA concerns. The repository abstraction becomes meaningless.

---

### 9.5 — No Rate Limiting

The API has no rate limiting configured at the application level. Endpoints like authentication and high-frequency data queries are fully exposed to brute-force and DoS attacks.

**Fix:** Add `builder.Services.AddRateLimiter(...)` with a sliding window or token bucket limiter. Combine with an API Gateway (e.g., NGINX, YARP, AWS API Gateway) for external rate limiting.

---

### 9.6 — No Health Checks for Dependencies

```csharp
app.MapGet("health/live", () => Results.Ok("Alive"));
```

Only a basic liveness check exists. There are no readiness checks for:
- PostgreSQL connectivity
- RabbitMQ connectivity
- SAP Middleware API reachability

**Fix:**
```csharp
services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq")
    .AddUrlGroup(middlewareApiUri, name: "sap-middleware");

app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
```

---

### 9.7 — `GetPrincipalFromExpiredToken` Returns `null` on Failure

**File:** `JWT.cs`

```csharp
public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
{
    try { ... return principal; }
    catch { return null; }  // ← returns null, callers may NullReferenceException
}
```

**Fix:** Return `Result<ClaimsPrincipal>` or throw a specific `InvalidTokenException`. Swallowing exceptions and returning null is a pattern that leads to NullReferenceExceptions in unexpected places.

---

### 9.8 — JWT Token Missing Issuer and Audience Claims

**File:** `JWT.cs`

```csharp
var accessTokenDescriptor = new SecurityTokenDescriptor
{
    Subject = GetSubject(payload),
    Expires = DateTime.UtcNow.AddMinutes(...),
    SigningCredentials = ...,
    // ⚠️ Issuer and Audience are NOT set here
};
```

`JwtOptions` contains `Issuer` and `Audience`, but they are never written into the token descriptor. The validator may be configured to check these, making all generated tokens invalid. Alternatively, if validation is lenient, it creates a security gap where tokens from other services could be accepted.

---

### 9.9 — Console.WriteLine Used for Service Registration Logging

Throughout `Resources.cs`, `DependencyInjection.cs`:

```csharp
Console.WriteLine($"[Info]----->all {nameof(RegisterService)} done");
```

This bypasses Serilog entirely and produces unstructured, untraceable output. In Docker/Kubernetes, this goes to stdout without structured context.

**Fix:** Use `ILogger<T>` injected at the appropriate level, or use a static `ILogger` bootstrapped before the host builds.

---

### 9.10 — OpenTelemetry OTLP Exporter Configuration is Inconsistent

**File:** `OpenTelemetryExtensions.cs`

The metrics and traces exporters are commented out and replaced with a conditional `UseOtlpExporter()` in `Program.cs`. The configuration mixes two styles and the tracing exporter has `AddRedisInstrumentation()` but Redis is commented out.

---

## 10. 🟢 Low-Severity / Design Observations

---

### 10.1 — Dead Code and Partially-Implemented Features

The following are present in the codebase but non-functional:

| Feature | Status | File |
|---------|--------|------|
| Redis Session Manager | Commented out | `Resources.cs` |
| Swagger (Swashbuckle) | Commented out, replaced with Scalar | `Resources.cs` |
| NATS Notification System | Commented out | `Resources.cs`, `Utility/NATSNotificationSystem/` |
| Custom Logger | Commented out | `Resources.cs` |
| gRPC clients | TODO comment only | `Resources.cs` |
| SuperAdmin Policy | Commented out | `Resources.cs` |
| Synchronous `Commit()` | Present but throws in transaction context | `UnitOfWork.cs` |
| S3 Helper | Present but not registered | `Utility/Helpers/S3Helper.cs` |
| EPPlus (Excel) | Package referenced, no usage found | `Directory.Packages.props` |
| SkiaSharp / QR Codes | Package referenced, no usage found | `Directory.Packages.props` |

These represent technical debt, bloat in the binary, and confusion for new developers.

---

### 10.2 — `Commit()` Synchronous Method Should Be Removed

```csharp
public int Commit()
{
    if (_transaction != null)
        throw new NotSupportedException("Synchronous commit when transaction is active...");
    return _db.SaveChanges();
}
```

In an async-first ASP.NET Core application, having a synchronous `SaveChanges()` on the public `IUnitOfWork` interface invites accidental thread-pool starvation. Remove it from the interface entirely.

---

### 10.3 — Auditing via Reflection is Fragile and Slow

**File:** `AppDbContext.cs`

The `HandleAuditing()` method uses `Type.GetProperty()` and `PropertyInfo.SetValue()` on every save call. For high-throughput operations, this reflection overhead is measurable.

**Fix:** Use an interface (`IAuditableEntity`) and cast entities directly:

```csharp
if (entry.Entity is IAuditableEntity auditable)
{
    if (entry.State == EntityState.Added)
        auditable.CreatedAt = now;
    // etc.
}
```

---

### 10.4 — `ApiResponseModel` Uses `Object` Type for `Data` and `Exception`

```csharp
public Object Data { get; set; } = new object();
public Object Exception { get; set; } = new List<string>();
```

These untyped properties lose IDE support, produce runtime type-checking burdens, and make API consumers uncertain about the shape of data they'll receive.

---

### 10.5 — Mixed JSON Serializers (System.Text.Json + Newtonsoft.Json)

`ApiResponseModel.ExecuteAsync()` uses **Newtonsoft.Json** while the rest of the application (Wolverine, ASP.NET Core) uses **System.Text.Json**. This is inconsistent and means two serialization stacks are active simultaneously, which can produce subtly different output for edge cases (null handling, date formats, enum serialization).

---

### 10.6 — No API Versioning Strategy

OpenAPI is registered as `"v1"` but there's no actual versioning mechanism. When breaking changes are needed, there's no path to introduce `v2` without a full rewrite of the routing layer.

**Fix:** Use `Asp.Versioning.Mvc` with the Minimal API extension to add route-based or header-based API versioning.

---

### 10.7 — Feature Classes Registered as `Transient` but Have No State

Features (e.g., `AddItem`, `GetItemById`) implement `IFeature` and are registered as `Transient` via assembly scan. Since they have no instance state (all methods are private static), this creates new instances on every resolution. They should be `Singleton`.

---

### 10.8 — No Pagination Defaults or Maximum Page Size

Pagination methods accept `PageSize` without a maximum cap:

```csharp
Task<(int total, List<TEntity>)> GetListWithPagination(..., int PageSize, ...);
```

A client requesting `PageSize=1000000` could exhaust memory. Enforce a maximum (e.g., 100 or 200).

---

### 10.9 — `EntityDto` Exposes Soft-Delete Internals to Clients

```csharp
public class EntityDto
{
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }   // ← should never be shown to external consumers
}
```

---

## 11. ✅ What Works Well

These design decisions are solid and should be preserved:

| Area | Observation |
|------|-------------|
| **WolverineFx Inbox/Outbox** | Durable message processing with PostgreSQL-backed inbox/outbox is correctly configured. This guarantees at-least-once delivery and survives process crashes. |
| **UUIDv7 for Primary Keys** | Generates time-sortable, globally unique IDs at the database level. Correct and modern choice. |
| **Features-as-Classes Pattern** | Single Responsibility per feature class with co-located validator. Clean, discoverable, easy to navigate. |
| **FluentValidation per Feature** | Validators are automatically registered and co-located with their feature. Cascade mode on rules is used correctly. |
| **OpenTelemetry Configured** | Tracing, metrics, and logging are all wired (even if OTLP export needs cleanup). |
| **Serilog Structured Logging** | Correctly configured with context enrichment. Seq integration for local dev is a good choice. |
| **ConcurrentDictionary for Repositories** | Repository instances are created lazily and thread-safely in UnitOfWork. |
| **WolverineFx Error Policies** | Retry with cooldown for HTTP exceptions, immediate dead-letter for API (4xx) exceptions — correct differentiation. |
| **Fanout Exchanges for SAP Events** | Decoupled consumers via fanout exchanges. Multiple consumers can independently subscribe to item events. |
| **DefaultIncomingMessage** | Correctly handles messages from non-Wolverine producers without envelope headers. |
| **snake_case Convention** | Consistent snake_case column naming applied globally via reflection in `OnModelCreating`. |

---

## 12. Enterprise-Level Roadmap

### Priority 1 — Immediate (Before Any Production Traffic)

| # | Action | Files Affected |
|---|--------|----------------|
| P1-1 | Rotate ALL credentials (RabbitMQ, DB, API keys). Remove all values from source control. | `appsettings.json`, `SapIntegrationOptions.cs`, `DA/DependencyInjection.cs` |
| P1-2 | Fix EF Core double `HasQueryFilter` bug — combine filters into one expression | `AppDbContext.cs` |
| P1-3 | Fix middleware ordering — GlobalExceptionHandler must be first | `ConfigureApp.cs` |
| P1-4 | Enable authentication in all environments (move policy bypass to test-only routes) | `ConfigureApp.cs`, `Resources.cs` |
| P1-5 | Remove hardcoded item codes from `SendSapDataChangedEventJob` | `SendSapDataChangedEventJob.cs` |
| P1-6 | Fix `CreatedBy` always-overwrite bug (uncomment the null check) | `AppDbContext.cs` |
| P1-7 | Fix validation HTTP status code from 402 → 422 | `RequestValidationFilter.cs` |
| P1-8 | Remove auto-migration from startup; migrate in CI/CD pipeline | `ConfigureApp.cs` |
| P1-9 | Fix refresh token — persist to DB and implement revocation | New `RefreshToken` entity + repository |

### Priority 2 — Short Term (Sprint 1–2)

| # | Action |
|---|--------|
| P2-1 | Add `Issuer` + `Audience` to JWT `SecurityTokenDescriptor` |
| P2-2 | Implement secrets management (Key Vault / environment injection) |
| P2-3 | Add health checks for Postgres, RabbitMQ, SAP Middleware |
| P2-4 | Fix CORS — define explicit allowed origins per environment |
| P2-5 | Add rate limiting (`AddRateLimiter`) |
| P2-6 | Remove dead code: NATS, Swagger stubs, commented Redis, test job data |
| P2-7 | Fix `SapItemHandler` upsert race condition with ON CONFLICT or distributed lock |
| P2-8 | Replace reflection-based auditing with `IAuditableEntity` interface |
| P2-9 | Replace `Console.WriteLine` with proper `ILogger` in DI registration |

### Priority 3 — Medium Term (Sprint 3–6)

| # | Action |
|---|--------|
| P3-1 | Eliminate Mediator from Wolverine handler path — unify to single dispatcher |
| P3-2 | Slim down `IRepository` interface — apply ISP, remove `IQueryable` exposure |
| P3-3 | Move DA auditing concern out of DbContext into a dedicated auditing service |
| P3-4 | Add API versioning strategy |
| P3-5 | Implement Redis caching for frequently-read, slowly-changing data (Items, UoM) |
| P3-6 | Standardize response types — introduce `Result<T>` pattern or ProblemDetails RFC 7807 |
| P3-7 | Add pagination maximums and defaults |
| P3-8 | Replace mixed JSON serializers — use System.Text.Json everywhere |

### Priority 4 — Enterprise Hardening (Sprint 7+)

| # | Action |
|---|--------|
| P4-1 | Integration test suite (WolverineFx TestMessageBus, EF Core InMemory + Testcontainers) |
| P4-2 | Distributed tracing correlation IDs propagated through RabbitMQ headers |
| P4-3 | API Gateway (YARP or AWS/Azure gateway) for external rate limiting, TLS termination |
| P4-4 | Kubernetes readiness/liveness probes tied to health check endpoints |
| P4-5 | Database connection pooling (PgBouncer or Npgsql built-in pooling configuration) |
| P4-6 | Outbox polling interval and dead-letter queue monitoring/alerting |
| P4-7 | RBAC with proper role claims and fine-grained authorization policies |
| P4-8 | Audit log as a separate append-only table (not just entity fields) |
| P4-9 | Feature flags for gradual rollout of SAP integration changes |

---

## 13. Target Enterprise Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         EXTERNAL BOUNDARY                                     │
│                                                                                │
│  ┌───────────┐   ┌──────────────┐   ┌──────────────────┐   ┌─────────────┐  │
│  │ SAP ERP   │   │ SAP Middleware│   │ Frontend / Mobile│   │ Other APIs  │  │
│  └─────┬─────┘   └──────┬───────┘   └────────┬─────────┘   └──────┬──────┘  │
└────────│────────────────│────────────────────│──────────────────────│─────────┘
         │                │                    │                      │
         │ AMQP            │ HTTP (mTLS)         │ HTTPS              │ HTTPS
         ▼                │                    ▼                      ▼
┌──────────────────────────│────────────────────────────────────────────────────┐
│                    API GATEWAY / REVERSE PROXY (YARP / NGINX)                 │
│  Rate Limiting · TLS Termination · Auth Token Introspection · Routing         │
└──────────────────────────│────────────────────────────────────────────────────┘
         │                │                    │                      │
         ▼                ▼                    ▼                      ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                         TAHAQ SERVICE (K8s Pod)                                │
│                                                                                │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ Middleware Pipeline (CORRECT ORDER):                                    │  │
│  │ GlobalExceptionHandler → HTTPS → CORS → Auth → UserContext → Endpoints │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                                │
│  ┌────────────────┐  ┌──────────────────┐  ┌────────────────────────────┐   │
│  │  HTTP Features  │  │ WolverineFx Bus   │  │  Background Services       │   │
│  │  (IFeature)     │  │  (Inbox/Outbox)   │  │  (Periodic jobs - config)  │   │
│  └───────┬─────────┘  └────────┬─────────┘  └────────────────────────────┘   │
│          │                     │                                               │
│  ┌───────▼─────────────────────▼──────────────────────────────────────────┐  │
│  │                      CORE DOMAIN                                         │  │
│  │  Features · Commands · Domain Services · Validators                      │  │
│  └──────────────────────────────┬──────────────────────────────────────────┘  │
│                                  │                                              │
│  ┌───────────────────────────────▼──────────────────────────────────────────┐ │
│  │                   DATA ACCESS (DA)                                         │ │
│  │  IUnitOfWork → Focused Repositories → AppDbContext (EF Core)               │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ┌─────────────────────────────────────────────────────────────────────────┐  │
│  │  OBSERVABILITY                                                            │  │
│  │  Serilog → OTLP → Grafana/Loki                                           │  │
│  │  OpenTelemetry Traces → Jaeger / Tempo                                   │  │
│  │  OpenTelemetry Metrics → Prometheus / Grafana                            │  │
│  │  Health Checks → /health/live + /health/ready                            │  │
│  └─────────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
         │                                        │
         ▼                                        ▼
┌─────────────────────────┐         ┌────────────────────────────────┐
│  PostgreSQL (Primary)   │         │  PostgreSQL (Read Replica)     │
│  · App schema           │         │  · Read-heavy queries          │
│  · wolverine schema     │         └────────────────────────────────┘
│  · audit_log schema     │
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐         ┌────────────────────────────────┐
│  RabbitMQ Cluster       │         │  Redis Cluster                 │
│  · item-created-exchange│         │  · Session tokens              │
│  · item-updated-exchange│         │  · Distributed locks (upsert)  │
│  · sap-inbound          │         │  · Response cache              │
│  · sap-outbound         │         └────────────────────────────────┘
│  · wolverine-dead-letter│
└─────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│  Secrets Management     │
│  Azure Key Vault /      │
│  AWS Secrets Manager /  │
│  HashiCorp Vault        │
└─────────────────────────┘
```

---

## 14. Technology Stack Assessment

| Component | Current | Recommendation | Reason |
|-----------|---------|----------------|--------|
| **Framework** | .NET 10 Minimal API | ✅ Keep | Latest LTS, excellent performance |
| **ORM** | EF Core 10 + Npgsql | ✅ Keep | Excellent PostgreSQL support |
| **Message Bus** | WolverineFx 5.x | ✅ Keep | Outstanding inbox/outbox, native .NET |
| **Message Broker** | RabbitMQ | ✅ Keep | Proven, good with WolverineFx |
| **Logging** | Serilog | ✅ Keep | Industry standard for .NET |
| **Observability** | OpenTelemetry | ✅ Keep | Vendor-neutral, correct choice |
| **Validation** | FluentValidation 12 | ✅ Keep | Best-in-class for .NET |
| **HTTP Client** | Refit | ✅ Keep | Clean, testable API clients |
| **Mediator** | Mediator (source gen) | ⚠️ Remove from Wolverine path | Redundant when Wolverine is present |
| **API Docs** | Scalar + OpenAPI | ✅ Keep | Modern alternative to Swagger UI |
| **In-Proc Messaging** | Mediator (non-Wolverine paths) | ✅ Keep for pure in-process commands | |
| **Response Serializer** | Mixed STJ + Newtonsoft | 🔴 Standardize on STJ | Consistency |
| **Auth** | JWT Bearer (custom) | ⚠️ Consider OIDC | For enterprise: use an Identity Provider (Keycloak, Auth0, Azure AD) |
| **Caching** | Redis (commented out) | 🔴 Enable | Already dependency-ready |
| **Session** | Redis (commented out) | 🔴 Enable or remove | Currently dead code |
| **gRPC** | Proto files + EndpointExposerGRPC | ⚠️ Evaluate need | If no gRPC consumers exist, remove the complexity |
| **NATS** | Commented out | 🔴 Remove entirely | Dead dependency pulling 2.7MB |

---

## Summary Score Card

| Category | Score | Notes |
|----------|-------|-------|
| **Security** | 2/10 | Live credentials in source, auth bypassed in dev |
| **Data Integrity** | 4/10 | Critical EF filter bug, upsert race condition |
| **Architecture Clarity** | 6/10 | Good patterns exist but are improperly ordered/configured |
| **Observability** | 7/10 | Good foundation, needs cleanup |
| **Resilience** | 7/10 | WolverineFx inbox/outbox is excellent |
| **Code Quality** | 5/10 | ISP violations, reflection auditing, dead code |
| **Testability** | 3/10 | No test projects, bloated interfaces, HTTP in DA layer |
| **Production Readiness** | 3/10 | Cannot deploy safely without P1 fixes |

---

*Document produced by architectural review of commit state as of 2026-03-18.*  
*All line references are to the current codebase state at time of review.*
