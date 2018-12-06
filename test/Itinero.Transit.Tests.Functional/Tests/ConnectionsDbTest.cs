using System;
using Itinero.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Performance;
using Serilog;

namespace Itinero.Transit.Tests.Functional.Tests
{
    public class ConnectionsDbTest : FunctionalTest
    {
        public override void Test()
        {
            // setup profile.
            var profile = Belgium.Sncb(new LocalStorage("cache"));

            // create a stops db and connections db.
            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();

            // load connections for the next day.
            Action loadConnections = () =>
            {
                connectionsDb.LoadConnections(profile, stopsDb, (DateTime.Now, new TimeSpan(1, 0, 0, 0)), out _);
            };
            loadConnections.TestPerf("Loading connections.");

            // enumerate connections by departure time.
            var tt = 0;
            var ce = 0;
            var departureEnumerator = connectionsDb.GetDepartureEnumerator();
            Action departureEnumeration = () =>
            {
                departureEnumerator.Reset();
                while (departureEnumerator.MoveNext())
                {
                    //var departureDate = DateTimeExtensions.FromUnixTime(departureEnumerator.DepartureTime);
                    //Log.Information($"Connection {departureEnumerator.GlobalId}: @{departureDate} ({departureEnumerator.TravelTime}s [{departureEnumerator.Stop1} -> {departureEnumerator.Stop2}])");
                    tt += departureEnumerator.TravelTime;
                    ce++;
                }
            };
            departureEnumeration.TestPerf("Enumerate by departure time", 10);
            Log.Information($"Enumerated {ce} connections! Sum is {tt}");
        }
    }
}