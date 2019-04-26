using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class MultiTransitDbTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            // Test case: we travel from Coiseaukaai to Ghent
            // We should take the centrum shuttle for that


            var osm = TransitDb.ReadFrom("CentrumbusBrugge2019-04-09.transitdb", 0);

            var lc = TransitDb.ReadFrom("fixed-test-cases-2019-04-09.transitdb", 1);

            var depCoiseauKaai = "https://www.openstreetmap.org/node/6348562147";
            var arrGent = "http://irail.be/stations/NMBS/008892007";
            var arrBruggeNMBS = "http://irail.be/stations/NMBS/008891009";
            var arrStationBrugge = "https://www.openstreetmap.org/node/6348496391";


            var tdbs = new List<TransitDb.TransitDbSnapShot>
            {
                osm.Latest,
                lc.Latest
            };

            var testDate = new DateTime(2019, 04, 09);

            var journey = tdbs.SelectProfile(new DefaultProfile())
                .SelectStops(depCoiseauKaai, arrGent)
                .SelectTimeFrame(testDate.AddHours(10), testDate.AddHours(16))
                .EarliestArrivalJourney();

            NotNull(journey);
            Serilog.Log.Information(journey.ToString(tdbs));
            return null;
        }
    }
}