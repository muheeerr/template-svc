using Newtonsoft.Json;
using System.Text;

namespace Utility.Logger
{
    public static class SlackExceptionMethods
    {
        private static readonly string _webhookUrl = DependencyInjection.SlackWebhook;
        private static string AppName="ageeks";

        public static async void AddException(Exception ex)
        {
            var httpClient = new HttpClient();

            var payload = new
            {
                text = $"Exception occurred at {AppName}: {ex.Message} at {DateTime.UtcNow}\nStack Trace:\n{ex.StackTrace}"
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            await httpClient.PostAsync(_webhookUrl, content);
        }
        public static async void AddException(string message)
        {
            var httpClient = new HttpClient();

            var payload = new
            {
                text = $"Exception occurred at {AppName}: {message}"
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
           await httpClient.PostAsync(_webhookUrl, content);
        }
    }
}
