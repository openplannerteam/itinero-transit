using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Processor.Switch.Read
{
    // ReSharper disable once InconsistentNaming
    internal class ReadOsmRelation : DocumentedSwitch, ITransitDbSource
    {
        private static readonly string[] _names =
            {"--read-open-street-map-relation", "--read-osm", "--rosm"};

        private static string About =
            "Creates a transit DB based on an OpenStreetMap-relation following the route scheme. For all information on Public Transport tagging, refer to [the OSM-Wiki](https://wiki.openstreetmap.org/wiki/Public_transport).n\n" +
            "A timewindow should be specified to indicate what period the transitDB should cover. \n\n" +
            "Of course, the relation itself should be provided. Either:\n\n - Pass the ID of the relation to download it\n - Pass the URL of a relation.xml\n - Pass the filename of a relation.xml\n\n" +
            "If the previous switch reads or creates a transit db as well, the two transitDbs are merged into a single one.\n\n" +
            "Note that this switch only downloads/reads the relation and keeps them in memory. To write them to disk, add --write-transit-db too.\n\n" +
            "Example usage to create the database:\n\n" +
            "        idp --create-transit-osm 9413958";


        private const bool IsStable = true;

        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("relation", "id",
                        "Either a number, an url (starting with http or https) or a path where the relation can be found"),
                    SwitchesExtensions.opt("window-start", "start",
                            "The start of the timewindow to load. Specify 'now' to take the current date and time.")
                        .SetDefault("now"),
                    SwitchesExtensions.opt("window-duration", "duration",
                            "The length of the window to load, in seconds. If zero is specified, no connections will be downloaded.")
                        .SetDefault("3600")
                };


        public ReadOsmRelation()
            : base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDb Generate(Dictionary<string, string> arguments)
        {
            var tdb = new TransitDb(0);
            
            var start = arguments.ParseDate("window-start");

            var durationSeconds = arguments.ParseTimeSpan("window-duration", start);

            start = start.ToUniversalTime();
            var end = start.AddSeconds(durationSeconds);
            var arg = arguments["relation"];
            Logger.LogAction =
                (origin, level, message, parameters) =>
                    Console.WriteLine($"[{DateTime.Now:O}] [{level}] [{origin}]: {message}");

            tdb.UseOsmRoute(arg, start, end);

            return tdb;
        }
    }
}