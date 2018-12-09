using System;

namespace Itinero.Transit.Logging
{
    public static class Log
    {
        /// <summary>
        /// Logs a message at the information level.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Information(string message)
        {
            Logger.Log(string.Empty, TraceEventType.Information, message);
        }
        
        /// <summary>
        /// Logs a message at the warning level.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Warning(string message)
        {
            Logger.Log(string.Empty, TraceEventType.Warning, message);
        }
        
        /// <summary>
        /// Logs a message at the warning level.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="message">The message.</param>
        public static void Warning(Exception ex, string message)
        {
            Logger.Log(string.Empty, TraceEventType.Warning, message + ex.ToString());
        }
        
        /// <summary>
        /// Logs a message at the error level.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Error(string message)
        {
            Logger.Log(string.Empty, TraceEventType.Error, message);
        }
    }
}