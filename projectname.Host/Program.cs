using projectname.Host.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

ValidateRequiredEnvironmentVariables();

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .Enrich.WithMachineName()
          .Enrich.WithEnvironmentName()
          .Enrich.WithProperty("Service", context.Configuration["OTEL_SERVICE_NAME"] ?? "projectname")
          .Enrich.WithProperty("Version", context.Configuration["OTEL_SERVICE_VERSION"] ?? "1.0.0")
          .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    var seqUrl = context.Configuration["SEQ_URL"];
    if (!string.IsNullOrEmpty(seqUrl))
    {
        config.WriteTo.Seq(seqUrl);
    }
});
builder.Services.RegisterService(builder.Configuration);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetryConfiguration(Assembly.GetAssembly(typeof(Program))!, builder.Configuration);

var app = builder.Build();

await app.Configure();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, exception) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/openapi/v1.json") ||
            httpContext.Request.Path.StartsWithSegments("/health/live"))
        {
            return Serilog.Events.LogEventLevel.Verbose;
        }

        return exception != null ? Serilog.Events.LogEventLevel.Error : Serilog.Events.LogEventLevel.Information;
    };
});

// Liveness probe — always responds OK if the process is running
app.MapGet("health/live", () => Results.Ok("Alive"))
    .ExcludeFromDescription();

// Readiness probe — checks PostgreSQL and Redis connectivity
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Full health — all checks, for internal dashboards only
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
}).RequireAuthorization();

app.Run();

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

// Make the implicit Program class visible to the test project
public partial class Program { }
