using System;

namespace Insight
{
    public class LogFactory
    {
        internal static ILogger GetLogger(Type type)
        {
            return ConsoleLogger.Instance;
        }
    }

    class ConsoleLogger : ILogger
    {
        private ConsoleLogger()
        {
        }

        public static ILogger Instance { get; } = new ConsoleLogger();

        public void LogFormat(LogType logType, object context, string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void LogException(Exception exception, object context)
        {
            Console.WriteLine(exception.ToString());
        }

        public ILogHandler logHandler { get; set; }
        public bool logEnabled { get; set; }
        public LogType filterLogType { get; set; }

        public bool IsLogTypeAllowed(LogType logType)
        {
            return true;
        }

        public void Log(LogType logType, object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(LogType logType, object message, object context)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(LogType logType, string tag, object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(LogType logType, string tag, object message, object context)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(string tag, object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void Log(string tag, object message, object context)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogWarning(string tag, object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogWarning(object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogWarning(string tag, object message, object context)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogError(string tag, object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogError(object message)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogError(string tag, object message, object context)
        {
            Console.WriteLine(message.ToString());
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            // don't know about this
        }

        public void LogException(Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }
    }

    public enum LogType
    {
        /// <summary>
        ///   <para>LogType used for Errors.</para>
        /// </summary>
        Error,

        /// <summary>
        ///   <para>LogType used for Asserts. (These could also indicate an error inside Unity itself.)</para>
        /// </summary>
        Assert,

        /// <summary>
        ///   <para>LogType used for Warnings.</para>
        /// </summary>
        Warning,

        /// <summary>
        ///   <para>LogType used for regular log messages.</para>
        /// </summary>
        Log,

        /// <summary>
        ///   <para>LogType used for Exceptions.</para>
        /// </summary>
        Exception,
    }

    public interface ILogHandler
    {
        /// <summary>
        ///   <para>Logs a formatted message.</para>
        /// </summary>
        /// <param name="logType">The type of the log message.</param>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        void LogFormat(LogType logType, Object context, string format, params object[] args);

        /// <summary>
        ///   <para>A variant of ILogHandler.LogFormat that logs an exception message.</para>
        /// </summary>
        /// <param name="exception">Runtime Exception.</param>
        /// <param name="context">Object to which the message applies.</param>
        void LogException(Exception exception, Object context);
    }

    public interface ILogger : ILogHandler
    {
        /// <summary>
        ///   <para>Set Logger.ILogHandler.</para>
        /// </summary>
        ILogHandler logHandler { get; set; }

        /// <summary>
        ///   <para>To runtime toggle debug logging [ON/OFF].</para>
        /// </summary>
        bool logEnabled { get; set; }

        /// <summary>
        ///   <para>To selective enable debug log message.</para>
        /// </summary>
        LogType filterLogType { get; set; }

        /// <summary>
        ///   <para>Check logging is enabled based on the LogType.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <returns>
        ///   <para>Retrun true in case logs of LogType will be logged otherwise returns false.</para>
        /// </returns>
        bool IsLogTypeAllowed(LogType logType);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(LogType logType, object message);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(LogType logType, object message, Object context);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(LogType logType, string tag, object message);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(LogType logType, string tag, object message, Object context);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(object message);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(string tag, object message);

        /// <summary>
        ///   <para>Logs message to the Unity Console using default logger.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        void Log(string tag, object message, Object context);

        /// <summary>
        ///   <para>A variant of Logger.Log that logs an warning message.</para>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        void LogWarning(string tag, object message);

        void LogWarning(object message);


        /// <summary>
        ///   <para>A variant of Logger.Log that logs an warning message.</para>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        void LogWarning(string tag, object message, Object context);

        /// <summary>
        ///   <para>A variant of ILogger.Log that logs an error message.</para>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        void LogError(string tag, object message);

        void LogError(object message);

        /// <summary>
        ///   <para>A variant of ILogger.Log that logs an error message.</para>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        void LogError(string tag, object message, Object context);

        /// <summary>
        ///   <para>Logs a formatted message.</para>
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void LogFormat(LogType logType, string format, params object[] args);

        /// <summary>
        ///   <para>A variant of ILogger.Log that logs an exception message.</para>
        /// </summary>
        /// <param name="exception"></param>
        void LogException(Exception exception);
    }
}