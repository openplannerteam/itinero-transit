using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Processor.Switch.Read
{
    // ReSharper disable once InconsistentNaming
    internal class ReadGTFS : DocumentedSwitch, ITransitDbSource
    {
        private static readonly string[] _names =
            {"--read-gtfs", "--rgtfs"};

        private static string About =
            "Creates a transit DB based on GTFS (or adds them to an already existing db), for the explicitly specified timeframe";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("path",
                        "The path of the GTFS archive"),
                    SwitchesExtensions.opt("window-start", "start",
                            "The start of the timewindow to load. Specify 'now' to take the current date and time. Otherwise provide a timestring of the format 'YYYY-MM-DDThh:mm:ss' (where T is a literal T). Special values: 'now' and 'today'")
                        .SetDefault("now"),
                    SwitchesExtensions.opt("window-duration", "duration",
                            "The length of the window to load, in seconds. If zero is specified, no connections will be downloaded. Special values: 'xhour', 'xday'")
                        .SetDefault("3600")
                };


        private const bool IsStable = true;


        public ReadGTFS()
            : base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDb Generate(Dictionary<string, string> arguments)
        {
            var tdb = new TransitDb( 0);
  
            var path = arguments["path"];


            var time = ParseDate(arguments["window-start"]);
            // In seconds
            var duration = ParseTimeSpan(arguments["window-duration"]);

            Console.WriteLine($"Loading GTFS file {path} in timewindow {time:s} + {duration} seconds");

            Logger.LogAction =
                (origin, level, message, parameters) =>
                    Console.WriteLine($"[{DateTime.Now:O}] [{level}] [{origin}]: {message}");

            tdb.UseGtfs(path, time, time.AddSeconds(duration));
            return tdb;
        }
    }
}