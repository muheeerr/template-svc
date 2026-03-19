using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Utility.Helpers.Auth.Models;
using Utility.Helpers.Common.Auth;

namespace Utility.Helpers.Auth.JWT
{
    public class JwtOptions
    {
        public required string Key { get; set; }
        public required int AccessTokenExpirationInMinutes { get; set; }
        public required int RefreshTokenExpirationInDays { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
    }

    public class Jwt(IOptions<JwtOptions> options, IConfiguration configuration)
    {
        public UserPayload GetUserPayloadFromClaims(ClaimsPrincipal user)
        {
            var userPayload = new UserPayload
            {
                Email = user.FindFirst(KAuthClaimTypes.Email)?.Value,
                UserId = user.FindFirst(KAuthClaimTypes.UserId)?.Value,
                UserType = user.FindFirst(KAuthClaimTypes.UserType)?.Value,
                RoleIds = user.FindFirst(KAuthClaimTypes.Resources)?.Value,
                SessionEndDate = user.FindFirst(KAuthClaimTypes.SessionEndDate)?.Value,
                SessionStartDate = user.FindFirst(KAuthClaimTypes.SessionStartDate)?.Value
            };

            return userPayload;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
        public AccessAndRefreshTokens GenerateToken(UserPayload payload)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(options.Value.Key);

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = GetSubject(payload),
                Expires = DateTime.UtcNow.AddMinutes(options.Value.AccessTokenExpirationInMinutes),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var refreshToken = GenerateRefreshToken();

            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);


            return new AccessAndRefreshTokens
            {
                AccessToken = tokenHandler.WriteToken(accessToken),
                RefreshToken = refreshToken
            };
        }

        private static ClaimsIdentity GetSubject(UserPayload payload)
        {
            return new ClaimsIdentity(new Claim[]
                            {
                    new Claim(KAuthClaimTypes.Email, payload.Email ?? string.Empty),
                    new Claim(KAuthClaimTypes.UserId, payload.UserId ?? string.Empty),
                    new Claim(KAuthClaimTypes.UserType, payload.UserType ?? string.Empty),
                    new Claim(KAuthClaimTypes.Resources, payload.RoleIds ?? string.Empty)
                            });
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(options.Value.Key);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
        public static SymmetricSecurityKey SecurityKey(string key) => new(Encoding.ASCII.GetBytes(key));
    }
}
