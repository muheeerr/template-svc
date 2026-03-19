using Core.Endpoints;
using DA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectname.Host.Middlewares;
using Scalar.AspNetCore;
using Utility.EndpointExposerGRPC.ResourceAndConfigMap;

namespace projectname.Host.Extensions
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

            // 3. Response compression (before exception handler so responses are compressed)
            app.UseResponseCompression();

            // 4. Correlation ID (before exception handler so errors include correlation ID)
            app.UseMiddleware<CorrelationIdMiddleware>();

            // 5. Global exception handler wraps everything below
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            // 6. Redirect HTTP → HTTPS
            app.UseHttpsRedirection();

            // 7. JWT validation
            app.UseAuthentication();

            // 8. Policy evaluation
            app.UseAuthorization();

            // 9. Populate IUserContext from claims (after auth, before endpoints)
            app.UseMiddleware<UserContextMiddleware>();

            // 10. Rate limiting
            app.UseRateLimiter();

            // 11. Output cache (before endpoint mapping)
            app.UseOutputCache();

            // 12. Map gRPC services
            app.GrpcServices();

            // 13. Terminal — map all feature endpoints
            app.MapEndpoints();

            // Database migrations
            await app.EnsureDatabaseCreated();

            // Seed development data
            await DA.Persistence.DbSeeder.SeedAsync(app.Services);

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
                    new ScalarServer($"{serverUrl}/projectname", Description: "UAT"),
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
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                logger.LogInformation("Applying EF Core migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database migration failed. Application cannot start.");
                throw;
            }
        }
    }
}
