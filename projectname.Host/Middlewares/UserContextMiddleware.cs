using Utility.Helpers.Auth.Models;
using Utility.Helpers.Auth;
using Utility.Helpers.Common;

namespace projectname.Host.Middlewares
{
    public interface IUserContext { UserPayload? Data { get; set; } }

    public class UserContext : IUserContext { public UserPayload? Data { get; set; } }

    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;

        public UserContextMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext context, IUserContext userContext)
        {
            var payload = HTTPContextUserRetriever.GetUserPayloadFromClaims(context.User);
            userContext.Data = payload;

            if (context.User.Identity?.IsAuthenticated == true && payload != null && !payload.IsValid())
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(
                    ApiResponseHelper.Failure("Invalid or incomplete JWT claims.", 401));
                return;
            }

            if (payload != null && DateTime.TryParse(payload.SessionStartDate, out DateTime time) && time > DateTime.UtcNow)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    ApiResponseHelper.Convert(true, false, $"Session will start after {time}", StatusCodes.Status403Forbidden, new object()));
                return;
            }

            await _next(context);
        }
    }

}

