using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Utility.Helpers.Auth.Models;
using Utility.Helpers.Common.Auth;

namespace Utility.Helpers.Auth
{
    public static class HTTPContextUserRetriever
    {
        public static string GetUserName(HttpContext httpContext)
        {
            var user = httpContext.User;
            var payload = GetUserPayloadFromClaims(user);
            return payload.UserId ?? string.Empty;
        }

        public static UserPayload GetUserPayloadFromClaims(ClaimsPrincipal user)
        {
            var userPayload = new UserPayload
            {
                SessionEndDate = user.FindFirst(KAuthClaimTypes.SessionEndDate)?.Value,
                SessionStartDate = user.FindFirst(KAuthClaimTypes.SessionStartDate)?.Value,
                Email = user.FindFirst(KAuthClaimTypes.Email)?.Value,
                UserId = user.FindFirst(KAuthClaimTypes.UserId)?.Value,
                UserType = user.FindFirst(KAuthClaimTypes.UserType)?.Value,
                RoleIds = user.FindFirst(KAuthClaimTypes.Resources)?.Value
            };

            return userPayload;
        }
    }
}
