namespace Utility.Helpers.Auth.Models
{
    public class UserPayload
    {
        public required string Email {  get; set; }
        public required string UserId { get; set; }
        public required string UserType { get; set; }
        public required string SessionStartDate { get; set; }
        public required string SessionEndDate { get; set; }
        public string RoleIds { get; set; } 

    }
    public class AccessAndRefreshTokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

    }
    public static class KTokenValidity
    {
        public static int RefreshTokenInMin { get; set; } = 10;
    }
    public static class KConstantToken { 
    public static string Separator = ";";
    }
}