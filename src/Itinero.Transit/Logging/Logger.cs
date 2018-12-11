using System.Collections.Generic;

namespace Itinero.Transit.Logging
{
    /// <summary>
    /// A logger.
    /// </summary>
    public class Logger
    {
        private readonly string _name;

        /// <summary>
        /// Creates a new logger.
        /// </summary>
        public Logger(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Creates a new logger.
        /// </summary>
        internal static Logger Create(string name)
        {
            return new Logger(name);
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        public void Log(TraceEventType type, string message, params object[] args)
        {
            if (Logger.LogAction == null)
            {
                Logger.LogAction = (o, level, localmessage, parameters) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[{o}] {level} - {localmessage}");
                };
            }

            Logger.LogAction(_name, type.ToString().ToLower(), string.Format(message, args), null);
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        public static void Log(string name, TraceEventType type, string message, params object[] args)
        {
            if (Logger.LogAction == null)
            {
                Logger.LogAction = (o, level, localmessage, parameters) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[{o}] {level} - {localmessage}");
                };
            }

            var toLog = args == null || args.Length == 0 ? message : string.Format(message, args);
            Logger.LogAction(name, type.ToString().ToLower(), toLog, null);
        }

        /// <summary>
        /// Defines the log action fuoriginoriginnction.
        /// </summary>
        /// <param name="origin">The origin of the message, a class or module name.</param>
        /// <param name="level">The level of the message, 'critical', 'error', 'warning', 'verbose' or 'information'.</param>
        /// <param name="message">The message content.</param>
        /// <param name="parameters">Any parameters that may be useful.</param>
        public delegate void LogActionFunction(string origin, string level, string message,
            Dictionary<string, object> parameters);

        /// <summary>
        /// Gets or sets the action to actually log a message.
        /// </summary>
        public static LogActionFunction LogAction { get; set; }
    }
}