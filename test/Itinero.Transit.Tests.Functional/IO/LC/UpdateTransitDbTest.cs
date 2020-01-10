using System;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the updating of connections.
    /// </summary>
    public class UpdateTransitDbTest : FunctionalTest
    {
        
        public override string Name => "Update Transit Db Test";

        protected override void Execute()
        {
            var tdb = new TransitDb(0);
            // setup profile.
            var profile = Belgium.Sncb();
            
            

            // load connections for the current day.
            var w = tdb.GetWriter();
            profile.AddAllLocationsTo(w);
            profile.AddAllConnectionsTo(w, DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime().AddHours(10));
            w.Close();
        }
    }
}