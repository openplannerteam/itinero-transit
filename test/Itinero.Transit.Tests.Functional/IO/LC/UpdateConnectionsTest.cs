using System;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the updating connections.
    /// </summary>
    public class UpdateConnectionsTest : FunctionalTest<TransitDb,
        (TransitDb transitDb, DateTime date, TimeSpan window)>
    {
        /// <summary>
        /// Gets the default update connections test.
        /// </summary>
        public static UpdateConnectionsTest Default => new UpdateConnectionsTest();
        
        protected override TransitDb Execute((TransitDb transitDb, DateTime date, TimeSpan window) input)
        {
            // setup profile.
            var profile = Belgium.Sncb(new LocalStorage("cache"));

            // load connections for the current day.
            input.transitDb.LoadConnections(profile, (input.date, input.window));

            return input.transitDb;
        }
    }
}