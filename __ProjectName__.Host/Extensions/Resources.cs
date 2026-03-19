using Core;
using Core.Endpoints;
using Core.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using __ProjectName__.Host.Extensions.Validators;
using __ProjectName__.Host.Middlewares;
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

namespace __ProjectName__.Host.Extensions
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

            Console.WriteLine($"[Info]----->all {nameof(RegisterService)} done");
            services.AddGrpc();

            var assembly = typeof(Health).Assembly;
            services.AddEndpointExposerGrpc("__ProjectName__", assembly, typeof(IFeature));

            // TODO: Register your gRPC clients here
            // Example:
            // services.AddGrpcClient<YourService.YourServiceClient>(o =>
            // {
            //     o.Address = new Uri("http://localhost:7089"); 
            // });

            services.AddOpenApi("v1", options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); });

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
            Console.WriteLine($"[Info]----->{nameof(AddRedisSessionManager)} service added");
            return services;
        }

        private static IServiceCollection AddMiddlewares(this IServiceCollection services)
        {
            services.AddScoped<IUserContext, UserContext>();
            Console.WriteLine($"[Info]----->{nameof(AddMiddlewares)} service added");
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
            Console.WriteLine($"[Info]----->{nameof(AddAuthDI)} service added");
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
