using StackExchange.Redis;

namespace Infrastructure.Redis
{
    public class RedisSessionManager
    {
        private readonly IDatabase _redisDatabase;
        private readonly IConnectionMultiplexer _redis;

        public RedisSessionManager(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
        }

        public async Task StoreTokenAsync(string userId, string token, TimeSpan expiration)
        {
            await _redisDatabase.StringSetAsync(userId, token, expiration);
        }

        public async Task<string?> GetTokenAsync(string userId)
        {
            return await _redisDatabase.StringGetAsync(userId);
        }

        public async Task<bool> RemoveTokenAsync(string userId)
        {
            return await _redisDatabase.KeyDeleteAsync(userId);
        }
    }
}
