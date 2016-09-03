using System;
using System.Collections.Generic;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Logging mediator class
    /// </summary>
    public static class LoggingMediator
    {
        /// <summary>
        /// Logs the specified text.
        /// </summary>
        /// <param name="logEvent">The event (text) to log.</param>
        /// <param name="type">Event type</param>
        public static void Log(string logEvent, LogEventType type = LogEventType.Information)
        {
            if (Loggers == null) return;

            foreach (var logger in Loggers)
                if (logger.TypeFilter == LogEventType.Undefined || ((logger.TypeFilter & type) == type))
                    logger.Log(logEvent, type);
        }

        /// <summary>
        /// Logs the specified event.
        /// </summary>
        /// <param name="logEvent">The event (object) to log.</param>
        /// <param name="type">Event type</param>
        public static void Log(object logEvent, LogEventType type = LogEventType.Information)
        {
            if (Loggers == null) return;

            foreach (var logger in Loggers)
                if (logger.TypeFilter == LogEventType.Undefined || ((logger.TypeFilter & type) == type))
                    logger.Log(logEvent, type);
        }

        /// <summary>
        /// Logs the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="type">Event type</param>
        public static void Log(Exception exception, LogEventType type = LogEventType.Exception)
        {
            if (Loggers == null) return;

            var exceptionText = ExceptionHelper.GetExceptionText(exception);

            foreach (var logger in Loggers)
                if (logger.TypeFilter == LogEventType.Undefined || ((logger.TypeFilter & type) == type))
                {
                    var exceptionLogger = logger as IExceptionLogger;
                    if (exceptionLogger != null)
                        exceptionLogger.Log(exception, type);
                    else
                        logger.Log(exceptionText, type);
                }
        }

        /// <summary>
        /// Logs the specified exception.
        /// </summary>
        /// <param name="leadingText">The leading text (inserted before the actual exception detail).</param>
        /// <param name="exception">The exception.</param>
        /// <param name="type">Event type</param>
        public static void Log(string leadingText, Exception exception, LogEventType type = LogEventType.Exception)
        {
            if (Loggers == null) return;

            var exceptionText = leadingText + "\r\n\r\n" + ExceptionHelper.GetExceptionText(exception);

            foreach (var logger in Loggers)
                if (logger.TypeFilter == LogEventType.Undefined || ((logger.TypeFilter & type) == type))
                {
                    var exceptionLogger = logger as IExceptionLogger;
                    if (exceptionLogger != null)
                        exceptionLogger.Log(leadingText, exception, type);
                    else
                        logger.Log(exceptionText, type);
        }
        }

        /// <summary>
        /// Adds a new logger which wishes to be notified whenever something needs to be logged.
        /// </summary>
        /// <param name="logger">The logger object.</param>
        public static void AddLogger(ILogger logger)
        {
            Loggers.Add(logger);
        }

        /// <summary>
        /// Removes all current loggers.
        /// </summary>
        public static void ClearLoggers()
        {
            if (Loggers == null) return;
            Loggers.Clear();
        }

        /// <summary>
        /// Internal list of loggers
        /// </summary>
        private static readonly List<ILogger> Loggers = new List<ILogger>();
    }

    /// <summary>
    /// Logger interface
    /// </summary>
    /// <remarks>
    /// Implement this interface for objects used for logging.
    /// </remarks>
    public interface ILogger
    {
        /// <summary>Logs the specified event (text).</summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        void Log(string logEvent, LogEventType type);

        /// <summary>Logs the specified event (object).</summary>
        /// <param name="logEvent">The event (object).</param>
        /// <param name="type">The event type.</param>
        void Log(object logEvent, LogEventType type);

        /// <summary>
        /// Gets or sets the type filter.
        /// </summary>
        /// <value>The type filter.</value>
        /// <remarks>
        /// Only events that match the type filter will be considered by this logger.
        /// </remarks>
        LogEventType TypeFilter { get; set; }
    }

    /// <summary>
    /// Interface for loggers that are capable of logging exceptions directly
    /// </summary>
    public interface IExceptionLogger
    {
        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="exception">The exception that is to be logged.</param>
        /// <param name="type">The event type.</param>
        void Log(Exception exception, LogEventType type);

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="leadingText">The leading text.</param>
        /// <param name="exception">The exception that is to be logged.</param>
        /// <param name="type">The event type.</param>
        void Log(string leadingText, Exception exception, LogEventType type);
    }

    /// <summary>
    /// Log event type
    /// </summary>
    [Flags]
    public enum LogEventType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,
        /// <summary>
        /// General information
        /// </summary>
        Information = 1,
        /// <summary>
        /// Warning
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Exception
        /// </summary>
        Exception = 4,
        /// <summary>
        /// Error
        /// </summary>
        Error = 8,
        /// <summary>
        /// Critical event
        /// </summary>
        Critical = 16,
        /// <summary>
        /// Success
        /// </summary>
        Success = 32
    }
}
