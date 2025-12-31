using System.Reflection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace __ProjectName__.Host.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryConfiguration(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            //.ConfigureResource(resource => resource.AddService("Oaken"))
            .WithMetrics(b => b
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsqlInstrumentation()
            //.AddOtlpExporter(otlpOptions =>
            //{
            //    otlpOptions.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")!);
            //    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            //})
            )
            .WithTracing(tracing =>
            {
                tracing.AddSource("__ProjectName__")
                .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddRedisInstrumentation();
                //.AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
                //.AddOtlpExporter(otlpOptions =>
                //{
                //    otlpOptions.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")!);
                //    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                //});
            });
        return services;
    }
}