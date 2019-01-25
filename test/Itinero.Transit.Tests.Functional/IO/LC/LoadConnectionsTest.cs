using System;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the load connections extension method.
    /// </summary>
    public class LoadConnectionsTest : FunctionalTest<(ConnectionsDb connections, StopsDb stops, TripsDb trips),
        (DateTime date, TimeSpan window)>
    {
        /// <summary>
        /// Gets the default location connections test.
        /// </summary>
        public static LoadConnectionsTest Default => new LoadConnectionsTest();

        protected override (ConnectionsDb connections, StopsDb stops, TripsDb trips) Execute(
            (DateTime date, TimeSpan window) input)
        {
            // setup profile.
            var profile = Belgium.Sncb();

            // create a stops db and connections db.
            var stopsDb = new StopsDb();
            var tripsDb = new TripsDb();
            var connectionsDb = new ConnectionsDb();

            // load connections for the current day.
            profile.AddDataTo(stopsDb, connectionsDb, tripsDb, input.date, input.date + input.window,
                Console.WriteLine);

            return (connectionsDb, stopsDb, tripsDb);
        }
    }
}