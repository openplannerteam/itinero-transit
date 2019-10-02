using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchJapanize : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--undo-delays","--japanize","--the-dutch-are-better","--swiss-perfection"};

        private static string _about =
            "Removes all the delays of the trips, so recreate the planned schedule.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool _isStable = true;


        public SwitchJapanize
            () :
            base(_names, _about, _extraParams, _isStable)
        {
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)
        {
            var filtered = new TransitDb(0);
            var wr = filtered.GetWriter();

            var stops = old.Latest.StopsDb.GetReader();
            while (stops.MoveNext())
            {
                wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
            }


            var connsEnumerator = old.Latest.ConnectionsDb.GetDepartureEnumerator();
            connsEnumerator.MoveTo(old.Latest.ConnectionsDb.EarliestDate);
            var c = new Connection();

            var trips = old.Latest.TripsDb;

            var delaySum = 0;

            while (connsEnumerator.MoveNext() )
            {
                connsEnumerator.Current(c);
                var tr = trips.Get(c.TripId);
                var newTripId = wr.AddOrUpdateTrip(tr.GlobalId);
                c.TripId = newTripId;


                c.DepartureTime -= c.DepartureDelay;
                delaySum += c.DepartureDelay;
                c.DepartureDelay = 0;

                c.ArrivalTime -= c.ArrivalDelay;
                c.ArrivalTime = 0;
                
                wr.AddOrUpdateConnection(c);

            }


            wr.Close();


           

            Console.WriteLine($"Removed {delaySum/60} minutes of delay. If only it was that easy in Belgium too...");
            return filtered;
        }
    }
}