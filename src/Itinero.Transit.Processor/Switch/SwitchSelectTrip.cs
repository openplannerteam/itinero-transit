using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor.Switch
{
    internal class SwitchSelectTrip : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--select-trip", "--filter-trip"};

        private static string _about =
            "Removes all connections form the database, except those of the specified trip ";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("id",
                        "The URI identifying the trip you want to keep")
                };

        private const bool _isStable = true;

        public SwitchSelectTrip() : base(_names, _about, _extraParams, _isStable)
        {
        }


        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)
        {
            var id = arguments["id"];


            var filtered = new TransitDb(0);
            var wr = filtered.GetWriter();


            var stops = old.Latest.StopsDb.GetReader();
            while (stops.MoveNext())
            {
                wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
            }


            var connsEnumerator = old.Latest.ConnectionsDb.GetDepartureEnumerator();
            connsEnumerator.MoveTo(old.Latest.ConnectionsDb.EarliestDate);
            var trips = old.Latest.TripsDb;
            var searched = trips.Get(id).Id;
            
            var c = new Connection();
            var copied = 0;

            var newTripId = wr.AddOrUpdateTrip(id);
            
            while (connsEnumerator.MoveNext())
            {
                connsEnumerator.Current(c);
                if (!c.TripId.Equals(searched))
                {
                    continue;
                }

                c.TripId = newTripId;
                wr.AddOrUpdateConnection(c);

                copied++;
            }

            

            wr.Close();


            if (copied == 0)
            {
                throw new Exception("There are no connections with the given tripId");
            }


            Console.WriteLine($"There are {copied} connections in trip {id}");
            return filtered;
        }
    }
}