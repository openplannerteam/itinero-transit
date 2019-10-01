using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    internal class SwitchNmbsFilter : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--sncb-filter", "--nmbs-filter"};

        private static string _about =
            "Legacy for NMBS. NMBS currently (anno 2019) doesn't correctly support platforms. This filter throws out all the useless locations which represent a single platform (URL which ends with _1, _2, ...)";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams = new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool _isStable = false;

        public SwitchNmbsFilter() : base(_names, _about, _extraParams, _isStable)
        {
        }

        private static readonly Regex _regex = new Regex("_[0-9]+$");

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)

        {
            var filtered = new TransitDb(old.DatabaseId);
            var wr = filtered.GetWriter();


            var stopIdMapping = new Dictionary<StopId, StopId>();

            var stops = old.Latest.StopsDb.GetReader();
            var copied = 0;
            while (stops.MoveNext())
            {
                if (_regex.IsMatch(stops.GlobalId) || stops.GlobalId.Contains("http://irail.be/stations/NMBS/00S"))
                {
                    continue;
                }


                var newId = wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
                var oldId = stops.Id;
                stopIdMapping.Add(oldId, newId);
                copied++;
            }

            if (copied == 0)
            {
                throw new Exception("There are no stops left");
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


            if (copied == 0)
            {
                Console.WriteLine("WARNING: There are no connections in this bounding box");
            }


            Console.WriteLine($"There are {stopCount} stops and {copied} connections in the bounding box");
            return filtered;
        }
    }
}