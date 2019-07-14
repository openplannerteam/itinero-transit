using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional
{
    public class Speedtest : FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
        {
            var date = new DateTime(2019, 06, 20, 0, 0, 0, DateTimeKind.Utc);
            var startLoading = DateTime.Now;
            var tdb = TransitDb.ReadFrom(new List<string>
            {
                "testdata/week/nmbs.latest.transitdb",
                "testdata/week/delijn-ant.latest.transitdb",
                "testdata/week/delijn-lim.latest.transitdb",
                "testdata/week/delijn-ovl.latest.transitdb",
                "testdata/week/delijn-vlb.latest.transitdb",
                "testdata/week/delijn-wvl.latest.transitdb",
            });
            var start = DateTime.Now;
            Information($"Loading from disk took {(start - startLoading).TotalMilliseconds}ms");
            var calc = tdb.SelectProfile(new DefaultProfile())
                .PrecalculateClosestStops()
                .SelectStops(Constants.Poperinge, Constants.Vielsalm)
                .SelectTimeFrame(date.AddHours(8), date.AddHours(18));
            calc.IsochroneFrom();
            var end = DateTime.Now;
            var timeNeeded = (end - start).TotalMilliseconds;
            var all = calc.AllJourneys();
            Information($"Took {timeNeeded}ms for {all.Count} solution");

            return input;
        }
    }
}