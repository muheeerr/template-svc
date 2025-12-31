namespace Utility.Helpers.Common.Auth
{
    public static class KAuthClaimTypes
    {
        public static string UserId { get; set; } = nameof(UserId);
        public static string UserType { get; set; } = nameof(UserType);
        public static string Resources { get; set; } = nameof(Resources);
        public static string Email { get; set; } = nameof(Email);
        public static string SessionEndDate { get; set; } = nameof(SessionEndDate);
        public static string SessionStartDate { get; set; } = nameof(SessionStartDate);
    }
}
