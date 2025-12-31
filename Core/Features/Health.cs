using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Utility.EndpointController;

namespace Core.Features
{
    public class Health(ILogger<Health> logger) : IFeature
    {
        private readonly ILogger<Health> _logger = logger;

        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/health/ready", Handle);
        }

        private IResult Handle()
        {
            _logger.LogInformation("Health check endpoint called");
            return Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow });
        }
    }
}
