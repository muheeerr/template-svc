namespace projectname.Host.Extensions;

/// <summary>
/// Extension methods for adding resilient HTTP clients with retry and circuit breaker policies.
/// Usage: services.AddResilientHttpClient("service-name", options => { ... });
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Registers a named HttpClient with standard resilience policies (retry, circuit breaker, timeout).
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>? configure = null)
    {
        var builder = services.AddHttpClient(name);

        if (configure is not null)
            builder.AddStandardResilienceHandler(configure);
        else
            builder.AddStandardResilienceHandler();

        return builder;
    }
}

