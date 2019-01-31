using System;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the load connections extension method.
    /// </summary>
    public class LoadTransitDbTest : FunctionalTest<TransitDb,
        (DateTime date, TimeSpan window)>
    {
        /// <summary>
        /// Gets the default location connections test.
        /// </summary>
        public static LoadTransitDbTest Default => new LoadTransitDbTest();

        protected override TransitDb Execute((DateTime date, TimeSpan window) input)
        {
            // setup profile.
            var profile = Belgium.Sncb();

            // create a stops db and connections db.
            var transitDb = new TransitDb();

            // load connections for the current day.
            var w = transitDb.GetWriter();
            profile.AddAllLocationsTo(w, Console.Error.WriteLine);
            profile.AddAllConnectionsTo(w, input.date, input.date + input.window,
                Console.WriteLine);
            w.Close();
            return transitDb;
        }
    }
}