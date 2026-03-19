using System.Diagnostics.Metrics;

namespace projectname.Host.Metrics;

public sealed class ApplicationMetrics
{
    public const string MeterName = "projectname.application";

    public readonly Counter<long> EmailsSent;
    public readonly Counter<long> EmailsFailed;
    public readonly Counter<long> S3Uploads;
    public readonly Counter<long> S3Failures;
    public readonly Counter<long> DatabaseErrors;
    public readonly Histogram<double> RequestDurationMs;

    public ApplicationMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName, "1.0");
        EmailsSent = meter.CreateCounter<long>("app.emails.sent");
        EmailsFailed = meter.CreateCounter<long>("app.emails.failed");
        S3Uploads = meter.CreateCounter<long>("app.s3.uploads");
        S3Failures = meter.CreateCounter<long>("app.s3.failures");
        DatabaseErrors = meter.CreateCounter<long>("app.db.errors");
        RequestDurationMs = meter.CreateHistogram<double>("app.request.duration_ms");
    }
}
