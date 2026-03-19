using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Utility.EndpointController;
using Utility.Helpers.Common;

namespace Core.Features.Tracing;

/// <summary>
/// Generates a three-span hierarchy (fast op → slow op → handled error) so you can verify
/// traces appear correctly in the Seq Traces tab.
/// Hit GET /v1/TraceDemo/TraceDemo, then open Seq → Traces and search by TraceId.
/// </summary>
public class TraceDemo : IFeature
{
    internal static readonly ActivitySource Source = new("Core.Tracing");

    public record Response(string TraceId, string SpanId, string[] Steps, long ElapsedMs);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(nameof(TraceDemo), Handle)
           .WithTags("Tracing")
           .WithDescription("Generates nested spans — open Seq Traces tab and search by the returned TraceId")
           .Produces<Response>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle()
    {
        var sw = Stopwatch.StartNew();
        var steps = new List<string>();

        using var root = Source.StartActivity("TraceDemo.Process");
        root?.SetTag("demo.type", "trace-visibility-test");

        // Step 1 — fast child span
        using (var step1 = Source.StartActivity("TraceDemo.Step1.FastOperation"))
        {
            step1?.SetTag("step", 1);
            await Task.Delay(20);
            step1?.AddEvent(new ActivityEvent("fast-operation-complete"));
            steps.Add("FastOperation (20 ms)");
        }

        // Step 2 — slow child span
        using (var step2 = Source.StartActivity("TraceDemo.Step2.SlowOperation"))
        {
            step2?.SetTag("step", 2);
            step2?.SetTag("simulated.latency_ms", 150);
            await Task.Delay(150);
            step2?.AddEvent(new ActivityEvent("slow-operation-complete",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection { ["items_processed"] = 42 }));
            steps.Add("SlowOperation (150 ms)");
        }

        // Step 3 — child span that records a handled exception
        using (var step3 = Source.StartActivity("TraceDemo.Step3.HandledError"))
        {
            step3?.SetTag("step", 3);
            try
            {
                throw new InvalidOperationException("Simulated transient error — intentionally handled");
            }
            catch (Exception ex)
            {
                step3?.SetStatus(ActivityStatusCode.Error, ex.Message);
                step3?.AddEvent(new ActivityEvent("exception",
                    DateTimeOffset.UtcNow,
                    new ActivityTagsCollection
                    {
                        ["exception.type"]       = ex.GetType().FullName ?? ex.GetType().Name,
                        ["exception.message"]    = ex.Message,
                        ["exception.stacktrace"] = ex.ToString()
                    }));
            }
            steps.Add("HandledError (recorded on span)");
        }

        root?.SetStatus(ActivityStatusCode.Ok);
        sw.Stop();

        var traceId = root?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? "no-trace";
        var spanId  = root?.SpanId.ToString()  ?? Activity.Current?.SpanId.ToString()  ?? "no-span";

        return Results.Ok(ApiResponseHelper.Success(
            new Response(traceId, spanId, [.. steps], sw.ElapsedMilliseconds),
            "Trace generated — search Seq Traces by the returned TraceId"));
    }
}
