using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace projectname.Host.Filters;

/// <summary>
/// Endpoint filter providing idempotency for POST/PUT operations.
/// Reads the "Idempotency-Key" header. On cache hit, returns the stored response.
/// On miss, executes the handler and caches the result for 24 hours.
/// Usage: .AddEndpointFilter&lt;IdempotencyFilter&gt;()
/// </summary>
public class IdempotencyFilter : IEndpointFilter
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private const string HeaderName = "Idempotency-Key";
    private const string KeyPrefix = "idempotency:";

    private readonly IDatabase _redis;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(IConnectionMultiplexer redis, ILogger<IdempotencyFilter> logger)
    {
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
            return await next(context);

        var cacheKey = $"{KeyPrefix}{key}";

        try
        {
            var cached = await _redis.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                _logger.LogInformation("Idempotency cache hit for key {IdempotencyKey}", key);
                var cachedResult = JsonSerializer.Deserialize<JsonElement>((string)cached!);
                return Results.Json(cachedResult, statusCode: StatusCodes.Status200OK);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for idempotency check {IdempotencyKey}. Proceeding without cache.", key);
        }

        var result = await next(context);

        try
        {
            var json = JsonSerializer.Serialize(result);
            await _redis.StringSetAsync(cacheKey, json, CacheDuration);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for idempotency store {IdempotencyKey}.", key);
        }

        return result;
    }
}
