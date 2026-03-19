using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Utility.Logger;

public class SlackExceptionLogger
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackExceptionLogger> _logger;
    private readonly string _webhookUrl;

    public SlackExceptionLogger(
        IHttpClientFactory factory,
        ILogger<SlackExceptionLogger> logger)
    {
        _httpClientFactory = factory;
        _logger = logger;
        _webhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL") ?? string.Empty;
    }

    public async Task LogExceptionAsync(Exception ex, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl)) return;

        try
        {
            var client = _httpClientFactory.CreateClient("slack");
            // Redacted summary only — full stack trace stays in Serilog/OTEL
            var payload = JsonSerializer.Serialize(new
            {
                text = $":rotating_light: *{ex.GetType().Name}* in `{ex.Source ?? "unknown"}`",
                attachments = new[]
                {
                    new
                    {
                        color = "danger",
                        fields = new[]
                        {
                            new { title = "Message", value = ex.Message, @short = false },
                            new { title = "Time", value = DateTime.UtcNow.ToString("u"), @short = true }
                        }
                    }
                }
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await client.PostAsync(_webhookUrl, content, ct);
        }
        catch (Exception inner)
        {
            _logger.LogWarning(inner, "Failed to send exception to Slack.");
        }
    }
}
