using Microsoft.AspNetCore.Http;
using Utility.Logger;

namespace Utility.Helpers.Common.Filters
{
    public class RequestLoggingFilter(ICustomLogger logger) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            logger.LogInformation("HTTP {Method} {Path} received", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return await next(context);
        }
    }
}