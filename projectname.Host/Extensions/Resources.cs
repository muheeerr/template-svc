using Core;
using Core.Endpoints;
using Core.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using projectname.Host.Extensions.Validators;
using projectname.Host.Middlewares;
using Serilog;
using StackExchange.Redis;
using System.Reflection;
using System.Threading.RateLimiting;
using Utility.EmailSender;
using Utility.EndpointController;
using Utility.EndpointExposerGRPC.ResourceAndConfigMap;
using Utility.Helpers;
using Utility.Helpers.Auth.JWT;
using Utility.Helpers.Auth.Models;
using Utility.Helpers.Common.Auth;
using Utility.Helpers.Common.Auth.Requirements;
using Utility.Helpers.ServiceCollectionExtensions;
using Utility.Logger;
using Infrastructure.Redis;
using Microsoft.AspNetCore.ResponseCompression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace projectname.Host.Extensions
{
    public static class ConfigDI
    {
        public static IServiceCollection RegisterService(this IServiceCollection services, IConfiguration configuration)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var featureType = typeof(IFeature);
            string validatorName = "RequestValidator";

            var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (allowedOrigins.Length == 0)
                throw new InvalidOperationException("ALLOWED_ORIGINS must contain at least one origin.");

            services
            .AddRedisSessionManager(configuration)
            .AddEndpoints()
            .AddMiddlewares()
            .AddAuthDI(configuration)
            .AddBusinessLayer(configuration)
            .AddHelpers(configuration)
            .AddValidatorUsingAssemblies(assemblies, featureType, validatorName, typeof(IValidator<>))
            .AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            })
            .AddMetrics()
            .AddEmailSender(configuration)
            .AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            Log.Information("[DI] All {ServiceName} completed", nameof(RegisterService));
            services.AddGrpc();

            var assembly = typeof(Health).Assembly;
            services.AddEndpointExposerGrpc("projectname", assembly, typeof(IFeature));

            // TODO: Register your gRPC clients here
            // Example:
            // services.AddGrpcClient<YourService.YourServiceClient>(o =>
            // {
            //     o.Address = new Uri("http://localhost:7089"); 
            // });

            services.AddOpenApi("v1", options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); options.AddDocumentTransformer<ApiVersionPathTransformer>(); });

            // API Versioning
            services.AddApiVersioningConfiguration();

            // Health checks
            services.AddHealthChecks()
                .AddNpgSql(
                    Environment.GetEnvironmentVariable("DBHost") ?? throw new InvalidOperationException("DBHost required"),
                    name: "postgresql",
                    tags: ["db", "ready"])
                .AddRedis(
                    Environment.GetEnvironmentVariable("RedisHost") ?? "localhost:6379",
                    name: "redis",
                    tags: ["cache", "ready"])
                .AddDbContextCheck<DA.Persistence.AppDbContext>(
                    name: "ef-core",
                    tags: ["db", "ready"]);

            // Response compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                    .Concat(new[] { "application/json", "application/grpc" });
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.SmallestSize;
            });

            // Global System.Text.Json configuration
            services.ConfigureHttpJsonOptions(opts =>
            {
                opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

            return services;
        }



        public static IServiceCollection AddRedisSessionManager(this IServiceCollection services, IConfiguration configuration)
        {
            var redisHost = Environment.GetEnvironmentVariable("RedisHost");
            ArgumentException.ThrowIfNullOrWhiteSpace(redisHost, "RedisHost environment variable is required.");

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisHost = Environment.GetEnvironmentVariable("RedisHost");
                ArgumentException.ThrowIfNullOrWhiteSpace(redisHost);

                var options = ConfigurationOptions.Parse(redisHost);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 5;

                return ConnectionMultiplexer.Connect(options);
            });

            services.AddScoped<RedisSessionManager>();
            Log.Information("[DI] {ServiceName} registered", nameof(AddRedisSessionManager));
            return services;
        }

        private static IServiceCollection AddMiddlewares(this IServiceCollection services)
        {
            services.AddScoped<IUserContext, UserContext>();
            Log.Information("[DI] {ServiceName} registered", nameof(AddMiddlewares));
            return services;
        }


        public static IServiceCollection AddAuthDI(this IServiceCollection services, IConfiguration configuration)
        {
            var key = Environment.GetEnvironmentVariable("JWT_KEY");
            ArgumentException.ThrowIfNullOrWhiteSpace(key, "JWT_KEY environment variable is required.");

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            ArgumentException.ThrowIfNullOrWhiteSpace(issuer, "JWT_ISSUER environment variable is required.");

            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            ArgumentException.ThrowIfNullOrWhiteSpace(audience, "JWT_AUDIENCE environment variable is required.");

            var accessTokenExpirationInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_IN_MINUTES") ?? "10");
            var refreshTokenExpirationInDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_IN_DAYS") ?? "10");

            services.Configure<JwtOptions>(options =>
            {
                options.Key = key;
                options.Issuer = issuer;
                options.Audience = audience;
                options.AccessTokenExpirationInMinutes = accessTokenExpirationInMinutes;
                options.RefreshTokenExpirationInDays = refreshTokenExpirationInDays;
            });

            services
            .AddJwtValidator(configuration, key, issuer, audience)
            .AddCustomAuthorization();

            services.AddTransient<Jwt>();
            Log.Information("[DI] {ServiceName} registered", nameof(AddAuthDI));
            return services;
        }

        private static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(KPolicyDescriptor.CustomPolicy, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new CustomAuthorizationRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, CustomAuthorizationHandler>();
            services.AddSingleton<Func<UserPayload, AccessAndRefreshTokens>>(sp =>
            {
                var jwt = sp.GetRequiredService<Jwt>();
                return (user) => jwt.GenerateToken(user);
            });
            return services;
        }
    }
}

