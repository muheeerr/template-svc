using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace projectname.Tests.Common;

public static class TestAuthHelper
{
    public static string GenerateTestJwt(
        string userId = "test-user-id",
        string userType = "Admin",
        string email = "test@test.com")
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-jwt-key-minimum-32-characters!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("UserId", userId),
            new Claim("UserType", userType),
            new Claim("Email", email),
        };

        var token = new JwtSecurityToken(
            issuer: "https://test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

