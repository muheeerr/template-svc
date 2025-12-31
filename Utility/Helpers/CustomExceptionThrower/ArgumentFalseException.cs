using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Utility.Helpers.CustomExceptionThrower
{
    public class ArgumentFalseException: ArgumentException
    {
        

        public ArgumentFalseException(string? paramName)
            : base( paramName)
        {
           
        }

        

        

        public static void ThrowIfFalse(bool argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (!argument)
            {
                Throw(paramName);
            }
        }
        [DoesNotReturn]
        internal static void Throw(string? paramName) =>
            throw new ArgumentFalseException(paramName);
    }
}
