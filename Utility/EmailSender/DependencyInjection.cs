using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Utility.EmailSender
{
    public static class DependencyInjection
    {
        
        public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
        {
           
            var senderEmail = Environment.GetEnvironmentVariable("SenderEmail");
            ArgumentException.ThrowIfNullOrWhiteSpace(senderEmail, "SenderEmail environment variable is required.");

            var senderPassword = Environment.GetEnvironmentVariable("SenderPassword");
            ArgumentException.ThrowIfNullOrWhiteSpace(senderPassword, "SenderPassword environment variable is required.");

            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            ArgumentException.ThrowIfNullOrWhiteSpace(smtpHost, "SMTP_HOST environment variable is required.");

            var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
            

            services.TryAddSingleton<IEmailService>(x => new EmailService(senderEmail, senderPassword, smtpHost, int.Parse(smtpPort)));
            Console.WriteLine($"[Info]----->{nameof(AddEmailSender)} service added");

            return services;
        }

        
    }
}
