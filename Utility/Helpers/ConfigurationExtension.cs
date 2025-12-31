using Microsoft.Extensions.Configuration;

namespace Utility.Helpers
{

    public interface IRead
    {
        string ReadFromAppsettings(string key);
    }
    public class Config : IRead
    {
        private readonly IConfiguration _configuration;
        public Config(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string ReadFromAppsettings(string key)
        {
            var apiKey = _configuration.GetSection(key).Value;
            if (apiKey == null)
            {
                throw new Exception($"Appsetings Key:{key} is missing.");
            }

            return apiKey;
        }
    }
}
