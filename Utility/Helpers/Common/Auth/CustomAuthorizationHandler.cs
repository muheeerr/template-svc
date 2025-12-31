using Microsoft.AspNetCore.Authorization;
using Utility.Helpers.Auth.Models;
using Utility.Helpers.Common.Auth.Requirements;
using Utility.CustomHTTP;
using Microsoft.AspNetCore.Http;
using Utility.Helpers.CommonRoles;
using Utility.Helpers.StringsExtension;
using Utility.Helpers.Common.Constant;
using Microsoft.Extensions.Hosting;

namespace Utility.Helpers.Common.Auth
{
    public class CustomAuthorizationHandler : AuthorizationHandler<CustomAuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _environment;
        
        public CustomAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomAuthorizationRequirement requirement)
        {
            // Optionally, check the request path

            var httpContext = context.Resource as HttpContext;
            if (httpContext != null)
            {
                var path = httpContext.Request.Path;
                if(!context?.User?.Identity?.IsAuthenticated ?? false)
                {
                    int statusCode = HTTPStatusCode400.Unauthorized;
                    _httpContextAccessor.HttpContext.Response.StatusCode = statusCode;
                    _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
                    await _httpContextAccessor.HttpContext.Response.WriteAsJsonAsync(ApiResponseHelper.Convert(true, false, $"Invalid jwt or token is expired", statusCode, null));
                    await _httpContextAccessor.HttpContext.Response.CompleteAsync();
                }
                // Optionally, check the request path
                else if (IsUserValidated(httpContext, _environment))
                {

                    context.Succeed(requirement);
                }
                else
                {
                    //TODO:loging
                    int statusCode = HTTPStatusCode400.Forbidden;
                    _httpContextAccessor.HttpContext.Response.StatusCode = statusCode;
                    _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
                    await _httpContextAccessor.HttpContext.Response.WriteAsJsonAsync(ApiResponseHelper.Convert(true, false, $"User is not authorized to access this resource: {path}", statusCode, null));
                    await _httpContextAccessor.HttpContext.Response.CompleteAsync();

                    context.Fail();

                }
            }
            else
            {
                //TODO:loging
                int statusCode = HTTPStatusCode500.ServiceUnavailable;
                _httpContextAccessor.HttpContext.Response.StatusCode = statusCode;
                _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
                await _httpContextAccessor.HttpContext.Response.WriteAsJsonAsync(ApiResponseHelper.Convert(true, false, "", statusCode, null));
                await _httpContextAccessor.HttpContext.Response.CompleteAsync();

                context.Fail();
            }

            
          
        }

        private static bool IsUserValidated(HttpContext context, IHostEnvironment environment)
        {

            var userTypeClaim = context.User.Claims.FirstOrDefault(x => x.Type == KAuthClaimTypes.UserType)?.Value;
            if (string.IsNullOrEmpty(userTypeClaim))
            {
                return false;
            }

            if (userTypeClaim == KDefinedRoles.SuperAdmin)
            {
                return true;
            }

            var resourceClaim = context.User.Claims.FirstOrDefault(x => x.Type == KAuthClaimTypes.Resources)?.Value;
            if (string.IsNullOrEmpty(resourceClaim))
            {
                return false;
            }

            var actions = resourceClaim.Split(KConstantToken.Separator);
            if (actions.Length == 0)
            {
                return false;
            }
            var currentUrl = context.Request.Path.Value?.ToLower() ?? string.Empty;

            var appNamePrefix = $"/{KConstant.ApiName.ToLower()}";

            // Normalize the current URL based on environment

            var allowed = actions.Any(x => x.Equals(currentUrl));
            return allowed;
        }
    }
}
