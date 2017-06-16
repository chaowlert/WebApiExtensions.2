using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NLog;

namespace WebApiExtensions.Services
{
    public static class TaskEx
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static Task Run(Action action,
            HttpRequestMessage request = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            return Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    var wrap = new TaskExException($"Task run error\n   at {member} in {file}:line {line}", ex);
                    var logEvent = LogEventInfo.Create(LogLevel.Error, "TaskEx", wrap, null, "Task run error");
                    if (request != null)
                        logEvent.Properties["request"] = request;
                    logger.Log(logEvent);
                }
            });
        }

        internal static Task CompletedTask { get; } = Task.Delay(0);
        public static Task Run(Func<Task> action,
            HttpRequestMessage request = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            return Task.Run(() =>
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    var wrap = new TaskExException($"Task run error\n   at {member} in {file}:line {line}", ex);
                    var logEvent = LogEventInfo.Create(LogLevel.Error, "TaskEx", wrap, null, "Task run error");
                    if (request != null)
                        logEvent.Properties["request"] = request;
                    logger.Log(logEvent);
                    return CompletedTask;
                }
            });
        }
    }

    public class TaskExException : Exception
    {
        public TaskExException()
        {
        }

        public TaskExException(string message) : base(message)
        {
        }

        public TaskExException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TaskExException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}