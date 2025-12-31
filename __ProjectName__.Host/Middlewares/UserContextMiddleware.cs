using Utility.Helpers.Auth.Models;
using Utility.Helpers.Auth;
using Utility.Helpers.Common;
using Utility.CustomHTTP;

namespace __ProjectName__.Host.Middlewares
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
            if (payload != null && DateTime.TryParse(payload.SessionStartDate, out DateTime time) && time > DateTime.UtcNow)
            {
                await context.Response.WriteAsJsonAsync(ApiResponseHelper.Convert(true, false, $"Session will be start after {time}", HTTPStatusCode400.Forbidden, new object()));
            }
            else
            {
                await _next(context);
            }

        }
    }

}
