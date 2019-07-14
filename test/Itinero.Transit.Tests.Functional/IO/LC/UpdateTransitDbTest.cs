using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the updating of connections.
    /// </summary>
    public class UpdateTransitDbTest : FunctionalTest<TransitDb,
        (TransitDb transitDb, DateTime date, TimeSpan window)>
    {
        /// <summary>
        /// Gets the default update connections test.
        /// </summary>
        public static UpdateTransitDbTest Default => new UpdateTransitDbTest();

        protected override TransitDb Execute((TransitDb transitDb, DateTime date, TimeSpan window) input)
        {
            // setup profile.
            var profile = Belgium.Sncb();

            // load connections for the current day.
            var w = input.transitDb.GetWriter();
            profile.AddAllLocationsTo(w);
            profile.AddAllConnectionsTo(w, input.date, input.date + input.window);
            w.Close();
            return input.transitDb;
        }
    }
}