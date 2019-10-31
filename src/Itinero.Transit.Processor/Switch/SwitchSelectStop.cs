using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchSelectStopById : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _attributesToTry =
        {
            "name", "name:nl", "name:en", "name:fr"
        };

        private static readonly string[] _names =
            {"--select-stop", "--select-stops", "--filter-stop", "--filter-stops"};

        private static string _about =
            "Filters the transit-db so that only stops with the given id(s) are kept. " +
            "All connections containing a removed location will be removed as well.\n\n" +
            "This switch is mainly used for fancy statistics.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("id", "ids", "The ';'-separated stops that should be kept"),

                    SwitchesExtensions.opt("allow-empty-connections",
                            "If flagged, the program will not crash if no connections are retained")
                        .SetDefault("false")
                };

        private const bool _isStable = true;

        public SwitchSelectStopById() :
            base(_names, _about, _extraParams, _isStable)
        {
        }


        private StopId FindStop(IStopsReader stops, string id)
        {
            if (stops.MoveTo(id))
            {
                return stops.Id;
            }

            stops.Reset();

            while (stops.MoveNext())
            {
                foreach (var attrKey in _attributesToTry)
                {
                    if (!stops.Attributes.TryGetValue(attrKey, out var name)) continue;
                    Console.WriteLine($"{attrKey} --> {name} ==? {id}");
                    if (name.ToLower().Equals(id.ToLower()))
                    {
                        return stops.Id;
                    }
                }
            }

            throw new ArgumentException($"The stop {id} could not be found");
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)

        {
            var ids = arguments["id"].Split(";");


            var allowEmptyCon = bool.Parse(arguments["allow-empty-connections"]);


            var filtered = new TransitDb(old.DatabaseId);
            var wr = filtered.GetWriter();


            var stopIdMapping = new Dictionary<StopId, StopId>();
            var searchedStops = new HashSet<StopId>();

            var stops = old.Latest.StopsDb.GetReader();

            StopId CopyStop(StopId stopId)
            {
                if (stopIdMapping.ContainsKey(stopId))
                {
                    return stopIdMapping[stopId];
                }

                stops.MoveTo(stopId);
                var newId = wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
                var oldId = stops.Id;
                stopIdMapping.Add(oldId, newId);
                return newId;
            }

            var copied = 0;
            foreach (var id in ids)
            {
                var stopId = FindStop(stops, id);
                CopyStop(stopId);
                searchedStops.Add(stopId);
                copied++;
            }

            if (copied == 0)
            {
                throw new ArgumentException("No stops found");
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
                    if (!(searchedStops.Contains(con.DepartureStop) || searchedStops.Contains(con.ArrivalStop)))
                    {
                        continue;
                    }

                    var trip = tripsDb.Get(con.TripId); // The old trip
                    var newTripId = wr.AddOrUpdateTrip(trip.GlobalId, trip.Attributes);

                    wr.AddOrUpdateConnection(
                        CopyStop(con.DepartureStop),
                        CopyStop(con.ArrivalStop),
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
                throw new ArgumentException("There are no connections which pass through this station");
            }


            Console.WriteLine($"There are {stopCount} stops and {copied} for the given stops");
            return filtered;
        }
    }
}