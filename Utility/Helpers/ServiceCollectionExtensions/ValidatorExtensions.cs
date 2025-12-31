using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Utility.Helpers.ServiceCollectionExtensions
{
    public static class ValidatorExtensions
    {
        public static IServiceCollection AddValidatorUsingAssemblies(this IServiceCollection services, Assembly[] assemblies, Type featureType, string validatorName, Type validatorType)
        {
            foreach (var assembly in assemblies)
            {
                var featureImplementations = assembly.GetTypes()
                    .Where(type => featureType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

                foreach (var implementation in featureImplementations)
                {

                    var nestedType = implementation.GetNestedType(validatorName);

                    if (nestedType != null)
                    {
                        // Find the interface implemented by the nested validator
                        var validatorInterface = nestedType.GetInterfaces()
                            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType);

                        if (validatorInterface != null)
                        {
                            services.AddTransient(validatorInterface, nestedType);
                        }
                    }
                }

            }

            Console.WriteLine($"[Info]----->{nameof(AddValidatorUsingAssemblies)} service added");
            return services;
        }

    }
}
