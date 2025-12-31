using Core;
using Core.Endpoints;
using Core.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using __ProjectName__.Host.Extensions.Validators;
using __ProjectName__.Host.Middlewares;
using StackExchange.Redis;
using System.Reflection;
using System.Runtime;
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
using Utility.NATSNotificationSystem;
using Utility.SessionManager;

namespace __ProjectName__.Host.Extensions
{
    public static class ConfigDI
    {
        public static IServiceCollection RegisterService(this IServiceCollection services, IConfiguration configuration)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var featureType = typeof(IFeature);
            string validatorName = "RequestValidator";
            services

            //.AddCustomLogger(configuration)
            .AddRedisSessionManager(configuration)
            .AddEndpoints()
            //.AddSwagger(KConstant.ApiName)
            .AddMiddlewares()
            //TODO: AddServicesLayers
            .AddAuthDI(configuration)
            .AddBusinessLayer(configuration)
            .AddHelpers(configuration)
            .AddNatsService(configuration)
            .AddValidatorUsingAssemblies(assemblies, featureType, validatorName, typeof(IValidator<>))
            .AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();

                });
            }).AddMetrics()
            .AddEmailSender(configuration)
            ;
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

            var RedisHost = Environment.GetEnvironmentVariable("RedisHost") ?? "localhost:6379";
            ArgumentNullException.ThrowIfNullOrEmpty(RedisHost, "please add env:RedisHost value");


            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configurationOptions = ConfigurationOptions.Parse(RedisHost, true);
                return ConnectionMultiplexer.Connect(configurationOptions);
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

        private static IServiceCollection AddSwagger(this IServiceCollection services, string pTitle)
        {
            //services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen(options =>
            //{
            //    options.SwaggerDoc("v1", new OpenApiInfo()
            //    {
            //        Title = pTitle,
            //        Version = "v1",
            //    });

            //    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            //    options.InferSecuritySchemes();
            //});
            Console.WriteLine($"[Info]----->{nameof(AddSwagger)} service added");
            return services;

        }
        public static IServiceCollection AddAuthDI(this IServiceCollection services, IConfiguration configuration)
        {

            var Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "asdavvasd132132131231232312312dsadasdsdsdsds@asd112";
            var Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "localhost";
            var Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "localhost";
            var AccessTokenExpirationInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_IN_MINUTES") ?? "10");
            var RefreshTokenExpirationInDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_IN_DAYS") ?? "10");


            services.Configure<JwtOptions>(options =>
            {
                options.Key = Key;
                options.Issuer = Issuer;
                options.Audience = Audience;
                options.AccessTokenExpirationInMinutes = AccessTokenExpirationInMinutes;
                options.RefreshTokenExpirationInDays = RefreshTokenExpirationInDays;
            });


            services

            .AddJwtValidator(configuration, Key, Issuer, Audience)
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
                // options.AddPolicy(KPolicyDescriptor.SuperAdminPolicy, policy=>policy.RequireAuthenticatedUser());
                options.AddPolicy(KPolicyDescriptor.CustomPolicy, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new CustomAuthorizationRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, CustomAuthorizationHandler>();
            services.AddSingleton<Func<UserPayload, AccessAndRefreshTokens>>(sp =>
            {
                var jwt = sp.GetRequiredService<Jwt>();
                return (user) => jwt.GenerateToken(user);
            });
            /* services.AddAuthorization(options =>
             {
                 options.AddPolicy(KPolicyDescriptor.SuperAdminPolicy, policy =>
                 {
                     policy.RequireAssertion(context =>
                     {
                         // Ensure the Resource is an HttpContext
                         if (context.Resource is HttpContext httpContext)
                         {
                             var roleManager = httpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
                             var userRoles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value);

                             foreach (var role in userRoles)
                             {
                                 var roleEntity = roleManager.FindByNameAsync(role).Result;
                                 if (roleEntity != null)
                                 {
                                     *//*var rolePolicies = roleEntity.Policies; // Assuming roleEntity has a Policies property

                                     if (rolePolicies.Any(p => p.Name == "RequiredPolicy"))
                                     {
                                         return true;
                                     }*//*
                                     return true;
                                 }
                             }
                         }

                         return false;
                     });
                 });
             });*/
            return services;
        }
    }
}
