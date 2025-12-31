namespace Utility.Helpers.CustomExceptionThrower
{


    public static class ArgumentThrowCustom
    {

        /// <summary>
        /// Throws a specified exception if the condition is true.
        /// </summary>
        /// <typeparam name="TException">The type of the exception to throw.</typeparam>
        
        /// <param name="message">The exception message.</param>
        public static void ThrowIfNull<TException>(this object obj, string message = null) where TException : Exception
        {
            if (obj is null)
            {
                if (message == null)
                {
                    throw (TException)Activator.CreateInstance(typeof(TException));
                }
                else
                {
                    throw (TException)Activator.CreateInstance(typeof(TException), message);
                }
            }
        }
    }

}
