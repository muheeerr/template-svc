namespace DA
{
    internal class EntityEnums
    {
        internal enum EntityStatus
        {
            Inactive = 0,
            Active = 1,
            Deleted = 2
        }

        // max length constants for string fields
        internal static class MaxLength
        {
            internal const int Name = 64;
            internal const int Description = 256;
            internal const int Address = 256;
            internal const int Contact = 20;
        }
    }
}
