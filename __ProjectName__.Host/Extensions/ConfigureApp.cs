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
            // Register global exception handler early in the pipeline to catch all exceptions

            //app.UseSerilogRequestLogging();
            //app.UseHttpMetrics();
            //app.UseMetricServer();
            app.GrpcServices();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapEndpoints();

            app.UseMiddleware<UserContextMiddleware>();
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            await app.EnsureDatabaseCreated();
            app.UseCors(x => x
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                .AddPreferredSecuritySchemes("BearerAuth")

                .AddHttpAuthentication("BearerAuth", auth =>
                {
                    auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
                });
                var serverUrl = Environment.GetEnvironmentVariable(variable: "SERVER_URL");
                options.Servers = new List<ScalarServer>()
                {
                    new ScalarServer($"{serverUrl}/__projectname__", Description: "UAT"),
                    new ScalarServer("http://localhost:5087", "Local http"),
                    new ScalarServer("https://localhost:7199", "Local https")
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
            // using var scope = app.Services.CreateScope();
            // var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // await db.Database.MigrateAsync();

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
            //{
            //    Console.WriteLine("Applying pending migrations...");
            //    await dbContext.Database.MigrateAsync(); // Apply pending migrations
            //}
            //else
            //{
            //    Console.WriteLine("No pending migrations found.");
            //}


        }
    }
}