using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the load connections extension method.
    /// </summary>
    public class LoadTransitDbTest : FunctionalTest<TransitDb,
        (DateTime date, TimeSpan window)>
    {
        private LinkedConnectionDataset profile;

        public LoadTransitDbTest(LinkedConnectionDataset profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Gets the default location connections test.
        /// </summary>
        public static LoadTransitDbTest Default => new LoadTransitDbTest(Belgium.Sncb());

        public static LoadTransitDbTest SncbDeLijn => new LoadTransitDbTest(new LinkedConnectionDataset(
            new List<LinkedConnectionDataset>
            {

                new LinkedConnectionDataset(
                    new Uri("https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/connections"),
                    new Uri("https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/stops"), new Downloader()),
                Belgium.Sncb(),
            }));


        protected override TransitDb Execute((DateTime date, TimeSpan window) input)
        {
            // create a stops db and connections db.
            var transitDb = new TransitDb();

            // load connections for the current day.
            var w = transitDb.GetWriter();
       
            profile.AddAllLocationsTo(w, Serilog.Log.Warning);
            profile.AddAllConnectionsTo(w, input.date, input.date + input.window,
                Serilog.Log.Warning);
            w.Close();
            return transitDb;
        }
    }
}