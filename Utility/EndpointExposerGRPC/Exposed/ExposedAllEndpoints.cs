using System.Reflection;

namespace Utility.EndpointExposerGRPC.Exposed
{
    public class ExposedAllEndpoints
    {
        public static List<string> GetAllActions(string ApiBaseName, Assembly assembly, Type iFeature)
        {
            var featureInterfaces = assembly.GetTypes()
                                                            .Where(t => t.IsInterface && iFeature.IsAssignableFrom(t) && t != iFeature)

                                                            .ToList();

            var routes = new List<string>();


            foreach (var features in featureInterfaces)
            {
                //var featureName = features.Name;
                var featureName = features != null && features.Name.StartsWith("I") 
                        && features.Name.Length > 1 
                        ? features.Name.Substring(1)
                        : features?.Name;

                var implementingClasses = assembly.GetTypes()
                                                  .Where(t => t.IsClass && !t.IsAbstract
                                                  && features!.IsAssignableFrom(t))
                                                  .ToList();

                foreach (var actions in implementingClasses)
                {
                    string actionName = actions.Name;

                    routes.Add($"/{featureName}/{actionName}");
                }


            }
            return routes;
        }
    }
}
