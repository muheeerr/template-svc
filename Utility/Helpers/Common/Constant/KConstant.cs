namespace Utility.Helpers.Common.Constant
{
    public static class KConstant
    {
        public const string ApiName = "Oaken";
    }
    public static class KConstantEnvironments
    {
        public const string Stagging = "Staging";
        public const string Production = "Production";
        public const string Local = "Local";
    }

    public static class AppConstants
    {
        public static readonly Guid BarrierStatusClosedId = new("019b0328-ba5f-7435-999f-3534dd229af5");
        public static readonly Guid BarrierStatusOpenId = new("019b0328-ba5f-7c84-bc54-7193cd44f6f2");
    }
}
