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
    public class LoadConnectionsTest : FunctionalTest<TransitDb,
        (DateTime date, TimeSpan window)>
    {
        /// <summary>
        /// Gets the default location connections test.
        /// </summary>
        public static LoadConnectionsTest Default => new LoadConnectionsTest();
        
        protected override TransitDb Execute((DateTime date, TimeSpan window) input)
        {
            // setup profile.
            var profile = Belgium.Sncb(new LocalStorage("cache"));

            // create a stops db and connections db.
            var transitDb = new TransitDb();

            // load connections for the current day.
            transitDb.LoadConnections(profile, (input.date, input.window));

            return transitDb;
        }
    }
}