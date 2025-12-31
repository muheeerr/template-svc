namespace Utility.Helpers.Common.Auth
{
    public static class KPolicyDescriptor
    {
        public static string SuperAdminPolicy { get; set; } = nameof(SuperAdminPolicy);

        public static string CustomPolicy { get; set; } = nameof(CustomPolicy);
    }
}
