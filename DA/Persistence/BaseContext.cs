using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;

namespace DA.Persistence
{
    public class BaseContext : DbContext
    {
        public BaseContext(DbContextOptions options)
            : base(options)
        {
        }

        public string GetUserName()
        {
            var httpContext = this.GetService<IHttpContextAccessor>()?.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return "SYSTEM";

            var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                     ?? httpContext.User.FindFirst("email")?.Value;

            return string.IsNullOrWhiteSpace(email) ? "SYSTEM" : email;
        }
    }
}
