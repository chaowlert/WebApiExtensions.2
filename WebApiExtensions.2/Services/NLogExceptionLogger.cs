using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using NLog;

namespace WebApiExtensions.Services
{
    public class NLogExceptionLogger : IExceptionLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var logEvent = LogEventInfo.Create(LogLevel.Error, "ExceptionLogger", context.Exception, null, "WebApi unhandled exception");
            logEvent.Properties["request"] = context.ExceptionContext.Request;
            logger.Log(logEvent);
            return TaskEx.CompletedTask;
        }
    }
}
