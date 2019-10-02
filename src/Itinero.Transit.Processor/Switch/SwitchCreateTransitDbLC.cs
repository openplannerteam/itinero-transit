using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Processor.Switch
{
    // ReSharper disable once InconsistentNaming
    internal class SwitchCreateTransitDbLC : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names =
            {"--create-transit-db-with-linked-connections", "--create-transit-lc", "--ctlc"};

        private static string _about =
            "Creates a transit DB based on linked connections (or adds them to an already existing db). For this, the linked connections source and a timewindow should be specified.\n" +
            "If the previous switch reads or creates a transit db as well, the two transitDbs are merged into a single one.\n\n" +
            "Note that this switch only downloads the connections and keeps them in memory. To write them to disk, add --write-transit-db too.\n\n" +
            "Example usage to create the database for the Belgian Railway (SNCB/NMBS):\n\n" +
            "        idp --create-transit-db https://graph.irail.be/sncb/connections https://irail.be/stations/NMBS";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("connections", "curl",
                        "The URL where connections can be downloaded. Special value: 'nmbs'"),
                    SwitchesExtensions.obl("locations", "stops","lurl",
                        "The URL where the location can be downloaded. Special value: 'nmbs'"),
                    SwitchesExtensions.opt("window-start", "start",
                            "The start of the timewindow to load. Specify 'now' to take the current date and time. Otherwise provide a timestring of the format 'YYYY-MM-DDThh:mm:ss' (where T is a literal T). Special values: 'now' and 'today'")
                        .SetDefault("now"),
                    SwitchesExtensions.opt("window-duration", "duration",
                            "The length of the window to load, in seconds. If zero is specified, no connections will be downloaded. Special values: 'xhour', 'xday'")
                        .SetDefault("3600")
                };


        private const bool _isStable = true;


        public SwitchCreateTransitDbLC()
            : base(_names, _about, _extraParams, _isStable)
        {
        }


        public TransitDb Generate(Dictionary<string, string> arguments)
        {
            var tdb = new TransitDb(0);
            Modify(arguments, tdb);
            return tdb;
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var curl = arguments["connections"];

            if (curl.Equals("nmbs"))
            {
                curl = "https://graph.irail.be/sncb/connections";
            }
            
            var lurl = arguments["locations"];
            if (lurl.Equals("nmbs"))
            {
                lurl = "https://graph.irail.be/sncb/stops";
            }
            var wStart = arguments["window-start"];
            var time = wStart.Equals("now")
                ? DateTime.Now
                : wStart.Equals("today")
                    ? DateTime.Now.Date
                    : DateTime.Parse(wStart);

            time = time.ToUniversalTime();
            // In seconds
            int duration;
            var durationStr = arguments["window-duration"];

            if (durationStr.EndsWith("day"))
            {
                duration = 24 * 60 * 60 * int.Parse(durationStr.Substring(0, durationStr.Length - 3));
            }
            else if (durationStr.EndsWith("hour"))
            {
                duration = 60 * 60 * int.Parse(durationStr.Substring(0, durationStr.Length - 4));
            }
            else
            {
                duration = int.Parse(durationStr);
            }


            Logger.LogAction =
                (origin, level, message, parameters) =>
                    Console.WriteLine($"[{DateTime.Now:O}] [{level}] [{origin}]: {message}");

            tdb.UseLinkedConnections(curl, lurl, time, time.AddSeconds(duration));
            return tdb;
        }
    }
}