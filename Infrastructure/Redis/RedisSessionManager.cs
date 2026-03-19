using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Redis
{
    public class RedisSessionManager
    {
        private readonly IDatabase _redisDatabase;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisSessionManager> _logger;

        public RedisSessionManager(IConnectionMultiplexer redis, ILogger<RedisSessionManager> logger)
        {
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
            _logger = logger;
        }

        public async Task StoreTokenAsync(string userId, string token, TimeSpan expiration)
        {
            try
            {
                await _redisDatabase.StringSetAsync(userId, token, expiration);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for StoreToken key {Key}. Operation skipped.", userId);
            }
        }

        public async Task<string?> GetTokenAsync(string userId)
        {
            try
            {
                return await _redisDatabase.StringGetAsync(userId);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for GetToken key {Key}. Returning default.", userId);
                return default;
            }
        }

        public async Task<bool> RemoveTokenAsync(string userId)
        {
            try
            {
                return await _redisDatabase.KeyDeleteAsync(userId);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for RemoveToken key {Key}. Returning false.", userId);
                return false;
            }
        }
    }
}
