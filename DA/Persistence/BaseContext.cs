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
        
        private static readonly string[] EmailClaimTypes =
        {
            ClaimTypes.Email,
            "email",
            "Email",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        };

        public string GetUserName()
        {
            var httpContext = this.GetService<IHttpContextAccessor>()?.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return "SYSTEM";

            var user = httpContext.User;

            var email = EmailClaimTypes
                .Select(type => user.FindFirst(type)?.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return email ?? "SYSTEM";
        }
    }
}
