using __ProjectName__.Host.Extensions;
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
    config.ReadFrom.Configuration(context.Configuration);
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

var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

if (useOtlpExporter)
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

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

app.MapGet("health/live", () => Results.Ok("Alive"));
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