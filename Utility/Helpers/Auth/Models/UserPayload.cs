namespace Utility.Helpers.Auth.Models;

public class UserPayload
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? UserType { get; set; }
    public string? RoleIds { get; set; }
    public string? SessionStartDate { get; set; }
    public string? SessionEndDate { get; set; }

    /// <summary>Returns true if the minimum required claims are present.</summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(UserId) &&
        !string.IsNullOrWhiteSpace(UserType);
}

public class AccessAndRefreshTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public static class KTokenValidity
{
    public static int RefreshTokenInMin { get; set; } = 10;
}

public static class KConstantToken
{
    public static string Separator = ";";
}