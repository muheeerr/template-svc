using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Utility.EndpointController;
using Utility.Helpers.Common.Constant;

namespace Core.Endpoints
{
    public static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services)
        {
            var assembly = Assembly.GetAssembly(typeof(EndpointExtensions));
            services.AddEndpoints(assembly!);
            return services;
        }

        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            ServiceDescriptor[] serviceDescriptors = assembly
                .DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                               type.IsAssignableTo(typeof(IFeature)))
                .Select(type => ServiceDescriptor.Transient(typeof(IFeature), type))
                .ToArray();

            services.TryAddEnumerable(serviceDescriptors);

            return services;
        }

        public static IApplicationBuilder MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null, string prefix = KConstant.ApiName)
        {
            IEnumerable<IFeature> endpoints = app.Services.GetRequiredService<IEnumerable<IFeature>>();

            IEndpointRouteBuilder builder = routeGroupBuilder == null ? app : routeGroupBuilder;

            var versionSet = builder.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .HasApiVersion(new ApiVersion(2, 0))
                .ReportApiVersions()
                .Build();

            foreach (IFeature endpoint in endpoints)
            {
                var featureInterface = endpoint.GetType().GetInterfaces()
            .FirstOrDefault(i => typeof(IFeature).IsAssignableFrom(i) && i != typeof(IFeature));

                // Use the interface name without the leading 'I' as the prefix
                var featureInterfaceName = featureInterface != null && featureInterface.Name.StartsWith("I") && featureInterface.Name.Length > 1
                    ? featureInterface.Name.Substring(1)
                    : featureInterface?.Name ?? endpoint.GetType().Name;
                var group = builder
                    .MapGroup($"/v{{version:apiVersion}}/{featureInterfaceName}")
                    .WithApiVersionSet(versionSet);
                endpoint.Map(group);
            }

            return app;
        }
    }
}
