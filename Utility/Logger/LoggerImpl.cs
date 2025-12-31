namespace Utility.Logger
{
    public class LoggerImpl :ICustomLogger
    {
        Serilog.Core.Logger logger;
        public LoggerImpl(Serilog.Core.Logger oLogger)
        {
            logger = oLogger;
        }
        public void LogWarning(string message, params object[] args)
        {
            logger.Warning(message, args);
        }
        public void LogInformation(string message, params object[] args)
        {
            logger.Information(message, args);
            // Log.CloseAndFlush();
        }
        public void LogError(string message, params object[] args)
        {
            logger.Error(message, args);
            Console.WriteLine($"LogError:{message}, args:{args}");
            //Log.CloseAndFlush();
            try
            {
               // SlackExceptionMethods.AddException(message);
            }
            catch(Exception ex)
            {
                logger.Error(ex.Message, ex);
                //Log.CloseAndFlush();
            }
        }
        public void LogErrors(Exception ex, string message, params object[] args)
        {
            logger.Error(ex, message, args);
            //Log.CloseAndFlush();
            try
            {
                SlackExceptionMethods.AddException(ex);
            }
            catch(Exception e)
            {
                logger.Error(e, e.Message);
                //Log.CloseAndFlush();
            }
        }
        public void LogError(Exception ex, string message)
        {
            logger.Error(ex, message);
            //Log.CloseAndFlush();
            try
            {
                SlackExceptionMethods.AddException(ex);
            }
            catch(Exception e)
            {
                logger.Error(e, message);
                //Log.CloseAndFlush();
            }
        }
    }
}
