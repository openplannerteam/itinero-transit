using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Filter
{
    internal class SelectStopsByBoundingBox : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--select-bounding-box", "--bounding-box", "--bbox"};

        private static string About =
            "Filters the transit-db so that only stops within the bounding box are kept. " +
            "All connections containing a removed location will be removed as well.\n\n" +
            "This switch is mainly used for debugging.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("left",
                        "Specifies the minimal latitude of the output."),
                    SwitchesExtensions.obl("right",
                        "Specifies the maximal latitude of the output."),
                    SwitchesExtensions.obl("top", "up",
                        "Specifies the minimal longitude of the output."),
                    SwitchesExtensions.obl("bottom", "down",
                        "Specifies the maximal longitude of the output."),

                    SwitchesExtensions.opt("allow-empty",
                            "If flagged, the program will not crash if no stops are retained")
                        .SetDefault("false"),
                    SwitchesExtensions.opt("allow-empty-connections",
                            "If flagged, the program will not crash if no connections are retained")
                        .SetDefault("false")
                };

        private const bool IsStable = true;

        public SelectStopsByBoundingBox() :
            base(_names, About, _extraParams, IsStable)
        {
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)

        {
            var minLon = float.Parse(arguments["left"]);
            var maxLon = float.Parse(arguments["right"]);
            var minLat = float.Parse(arguments["bottom"]);
            var maxLat = float.Parse(arguments["top"]);


            var allowEmpty = bool.Parse(arguments["allow-empty"]);
            var allowEmptyCon = bool.Parse(arguments["allow-empty-connections"]);


            var newDb = old.Copy(
                allowEmptyCon,
                keepStop: stop =>
                {
                    var lon = stop.Longitude;
                    var lat = stop.Latitude;
                    return minLat <= lat && lat <= maxLat && minLon <= lon && lon <= maxLon;
                },
                keepTrip: _ => false,
                keepConnection: x =>
                {
                    var c = x.c;
                    var stopMapping = x.reverseStopIdMapping;
                    return stopMapping.ContainsKey(c.DepartureStop) && stopMapping.ContainsKey(c.ArrivalStop);
                }
            );


            var newStopCount = newDb.Latest.StopsDb.Count();
            if (!allowEmpty && newStopCount == 0)
            {
                throw new Exception("There are no stops in the selected bounding box");
            }


            var removed = old.Latest.StopsDb.Count() - newStopCount;
            Console.WriteLine($"There are {newStopCount} stops (removed {removed} stops) in the bounding box");
            return newDb;
        }
    }
}