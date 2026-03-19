namespace Utility.Logger
{
    public class LoggerImpl : ICustomLogger
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
        }
        public void LogError(string message, params object[] args)
        {
            logger.Error(message, args);
            Console.WriteLine($"LogError:{message}, args:{args}");
        }
        public void LogErrors(Exception ex, string message, params object[] args)
        {
            logger.Error(ex, message, args);
            // TODO: Inject SlackExceptionLogger via DI to send Slack notifications
        }
        public void LogError(Exception ex, string message)
        {
            logger.Error(ex, message);
            // TODO: Inject SlackExceptionLogger via DI to send Slack notifications
        }
    }
}
