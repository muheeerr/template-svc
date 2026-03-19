using Core.Endpoints;
using DA.Persistence;
using Microsoft.EntityFrameworkCore;
using __ProjectName__.Host.Middlewares;
using Scalar.AspNetCore;
using Utility.EndpointExposerGRPC.ResourceAndConfigMap;

namespace __ProjectName__.Host.Extensions
{
    public static class ConfigureApp
    {
        public static async Task Configure(this WebApplication app)
        {
            // 1. CORS must be first — handles preflight OPTIONS requests
            app.UseCors();

            // 2. Security headers on every response
            app.Use(async (ctx, next) =>
            {
                ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
                ctx.Response.Headers["X-Frame-Options"] = "DENY";
                ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                ctx.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
                await next();
            });

            // 3. Global exception handler wraps everything below
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            // 4. Redirect HTTP → HTTPS
            app.UseHttpsRedirection();

            // 5. JWT validation
            app.UseAuthentication();

            // 6. Policy evaluation
            app.UseAuthorization();

            // 7. Populate IUserContext from claims (after auth, before endpoints)
            app.UseMiddleware<UserContextMiddleware>();

            // 8. Rate limiting
            app.UseRateLimiter();

            // 9. Map gRPC services
            app.GrpcServices();

            // 10. Terminal — map all feature endpoints
            app.MapEndpoints();

            // Database migrations
            await app.EnsureDatabaseCreated();

            // OpenAPI / Scalar documentation
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                .AddPreferredSecuritySchemes("BearerAuth")
                .AddHttpAuthentication("BearerAuth", auth =>
                {
                    auth.Token = Environment.GetEnvironmentVariable("SCALAR_BEARER_TOKEN") ?? string.Empty;
                });

                var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL");
                options.Servers = new List<ScalarServer>()
                {
                    new ScalarServer($"{serverUrl}/__projectname__", Description: "UAT"),
                    new ScalarServer("https://localhost:5087", "Local https"),
                    new ScalarServer("http://localhost:5088", "Local http")
                };
            });
        }

        public static void GrpcServices(this WebApplication app)
        {
            app.MapEndpointsExposed();
            // TODO: Add your gRPC service mappings here
            // Example: app.MapGrpcService<YourGrpcService>();
        }

        private static async Task EnsureDatabaseCreated(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}