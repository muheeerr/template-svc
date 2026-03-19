using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using DA.Persistence;

namespace projectname.Tests.Common;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Set env vars BEFORE the host starts (ValidateRequiredEnvironmentVariables runs at startup)
        Environment.SetEnvironmentVariable("DBHost", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RedisHost", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_KEY", "test-jwt-key-minimum-32-characters!!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "https://test-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
        Environment.SetEnvironmentVariable("ALLOWED_ORIGINS", "https://localhost:3000");
        Environment.SetEnvironmentVariable("SenderEmail", "test@test.com");
        Environment.SetEnvironmentVariable("SenderPassword", "test-password");
        Environment.SetEnvironmentVariable("AES_KEY", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        Environment.SetEnvironmentVariable("SMTP_HOST", "localhost");
        Environment.SetEnvironmentVariable("SMTP_PORT", "1025");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

