using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    internal class SwitchSelectStopsByBoundingBox : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--bounding-box", "--bb"};

        private static string _about =
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

        private const bool _isStable = true;

        public SwitchSelectStopsByBoundingBox() :
            base(_names, _about, _extraParams, _isStable)
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


            var filtered = new TransitDb(old.DatabaseId);
            var wr = filtered.GetWriter();


            var stopIdMapping = new Dictionary<StopId, StopId>();

            var stops = old.Latest.StopsDb.GetReader();
            var copied = 0;
            while (stops.MoveNext())
            {
                var lat = stops.Latitude;
                var lon = stops.Longitude;
                if (
                    !(minLat <= lat && lat <= maxLat && minLon <= lon && lon <= maxLon))
                {
                    continue;
                }

                var newId = wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
                var oldId = stops.Id;
                stopIdMapping.Add(oldId, newId);
                copied++;
            }

            if (!allowEmpty && copied == 0)
            {
                throw new Exception("There are no stops in the selected bounding box");
            }


            var consDb = old.Latest.ConnectionsDb;
            var tripsDb = old.Latest.TripsDb;

            var stopCount = copied;
            copied = 0;

            var first = consDb.First();
            if (first.HasValue)
            {
                var index = first.Value;
                do
                {
                    var con = consDb.Get(index);
                    if (!(stopIdMapping.ContainsKey(con.DepartureStop) && stopIdMapping.ContainsKey(con.ArrivalStop)))
                    {
                        // One of the stops is outside of the bounding box
                        continue;
                    }

                    var trip = tripsDb.Get(con.TripId); // The old trip
                    var newTripId = wr.AddOrUpdateTrip(trip.GlobalId, trip.Attributes);

                    wr.AddOrUpdateConnection(
                        stopIdMapping[con.DepartureStop],
                        stopIdMapping[con.ArrivalStop],
                        con.GlobalId,
                        con.DepartureTime.FromUnixTime(),
                        con.TravelTime,
                        con.DepartureDelay,
                        con.ArrivalDelay,
                        newTripId,
                        con.Mode
                    );
                    copied++;
                } while (consDb.HasNext(index, out index));
            }

            wr.Close();


            if (!allowEmptyCon && copied == 0)
            {
                throw new Exception("There are no connections in this bounding box");
            }


            Console.WriteLine($"There are {stopCount} stops and {copied} connections in the bounding box");
            return filtered;
        }
    }
}