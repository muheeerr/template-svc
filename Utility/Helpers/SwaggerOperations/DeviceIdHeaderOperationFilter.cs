/*    using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Helpers.CustomSwaggerOperations
{
    public class DeviceIdHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "DeviceId",
                In = ParameterLocation.Header,
                Required = false, // Set to true if the header is required
                Description = "This is a custom header",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}

*/