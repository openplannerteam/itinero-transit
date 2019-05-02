using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Journeys;

// ReSharper disable UnusedMember.Local

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class MultiTransitDbTest : FunctionalTest<object, object>
    {
        private const string _osmCentrumShuttle0409 = "CentrumbusBrugge2019-04-09.transitdb";
        private const string _nmbs0409 = "fixed-test-cases-2019-04-09.transitdb";

        private const string _osmCentrumShuttle0429 = "CentrumbusBrugge2019-04-29.transitdb";
        private const string _nmbs0429 = "fixed-test-cases-2019-04-29.transitdb";
        private const string _delijnWvl0429 = "fixed-test-cases-de-lijn-wvl-2019-04-29.transitdb";
        private const string _delijnOVl0429 = "fixed-test-cases-de-lijn-ovl-2019-04-29.transitdb";
        private const string _delijnVlB0429 = "fixed-test-cases-de-lijn-vlb-2019-04-29.transitdb";
        private const string _delijnLim0429 = "fixed-test-cases-de-lijn-lim-2019-04-29.transitdb";
        private const string _delijnAnt0429 = "fixed-test-cases-de-lijn-ant-2019-04-29.transitdb";

        private const string _coiseauKaaiOsm = "https://www.openstreetmap.org/node/6348562147";
        private const string _gentNmbs = "http://irail.be/stations/NMBS/008892007";
        private const string _bruggeNmbs = "http://irail.be/stations/NMBS/008891009";
        private const string _stationBruggeOsm = "https://www.openstreetmap.org/node/6348496391";

        private const string _gentZwijnaardeDeLijn = "https://data.delijn.be/stops/200657";

        private void MultiModalWithOsm(List<TransitDb.TransitDbSnapShot> tdbs, string dep, string arr, DateTime date,
            int iterations = 1)
        {
            // Test case: we travel from Coiseaukaai to Ghent
            // We should take the centrum shuttle for that


            var profiled = tdbs.SelectProfile(new DefaultProfile());
            var cacher = (StopSearchCaching) profiled._stopsReader;

            var settings = profiled
                .PrecalculateClosestStops()
                .SelectStops(dep, arr)
                .SelectTimeFrame(date.AddHours(10), date.AddHours(13));

            cacher.DumpCacheTotals();
            cacher.ResetCounters();

            var start = DateTime.Now;
            var journey = settings.IsochroneFrom();

            cacher.DumpCacheTotals();
            cacher.ResetCounters();

            var end = DateTime.Now;
            var totalSeconds = (end - start).TotalMilliseconds;
            Serilog.Log.Information(
                $"Calculating route took {totalSeconds}ms for {iterations} iterations, {totalSeconds / iterations}ms on average");


            List<Journey<TransferMetric>> journeys = new List<Journey<TransferMetric>>();
            start = DateTime.Now;
            for (var i = 0; i < iterations; i++)
            {

                var strt = DateTime.Now;
                journeys = settings.AllJourneys();
                var nd = DateTime.Now;
                Serilog.Log.Information($"Iteration took {(nd - strt).TotalMilliseconds}");
                cacher.DumpCacheTotals();
                cacher.ResetCounters();
            }

            end = DateTime.Now;
            totalSeconds = (end - start).TotalSeconds;

            Serilog.Log.Information(
                $"Calculating all routes took {totalSeconds}ms for {iterations} iterations, {totalSeconds / iterations}ms on average");

/*    
            foreach (var j in journeys)
            {
                Serilog.Log.Information(
                    $"Journey option: {j.Root.Time.FromUnixTime():hh:mm:ss} --> {j.Time.FromUnixTime():hh:mm:ss}, {j.Metric}");
            }

            foreach (var j in journeys)
            {
                Serilog.Log.Information($"Journey: {j.Summarized().ToString(tdbs)}");
            }
//*/
            Serilog.Log.Information($"Found {journeys.Count} options");
        }

        protected override object Execute(object input)
        {
            /*            MultiModalWithOsm(new List<string> {_osmCentrumShuttle0409, _nmbs0409}, _coiseauKaaiOsm, _gentNmbs,
                        new DateTime(2019, 04, 09));
                    
                    MultiModalWithOsm(new List<string> {_osmCentrumShuttle0429, _nmbs0429}, _coiseauKaaiOsm, _gentNmbs,
                        new DateTime(2019, 04, 29));
        //*/
            var dbs = new List<string>
            {
                _osmCentrumShuttle0429, _nmbs0429,
                _delijnAnt0429, _delijnLim0429,
                _delijnWvl0429, _delijnOVl0429,
                _delijnVlB0429
            };

            var tdbs = new List<TransitDb.TransitDbSnapShot>();
            for (uint i = 0; i < dbs.Count; i++)
            {
                var tdb = TransitDb.ReadFrom(dbs[(int) i], i);
                tdbs.Add(tdb.Latest);
            }


            MultiModalWithOsm(tdbs, _coiseauKaaiOsm, _gentZwijnaardeDeLijn,
                new DateTime(2019, 04, 29), 50);

            return null;
        }
    }
}