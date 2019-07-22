using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Staging;

namespace Itinero.Transit.Tests.Functional.FullStack
{
    public class FullStackTest : FunctionalTest<object, (string from, string to, uint range)>
    {
        public List<(string, string, uint)> TestLocations = new List<(string, string, uint)>
        {
           (Constants.OsmNearStationBruggeLatLon, Constants.Gent, 1000),
           (Constants.Brugge, Constants.OsmDeSterre, 2500),
           (Constants.OsmNearStationBruggeLatLon, Constants.OsmDeSterre, 5000),
           (Constants.OsmNearStationBruggeLatLon, Constants.OsmHermanTeirlinck, 5000),
            (Constants.OsmHermanTeirlinck, Constants.OsmDeSterre, 5000),
            (Constants.OsmWechel, Constants.OsmDeSterre, 25000),
        };


        public void TestAll()
        {
            var i = 0;
            foreach (var input in TestLocations)
            {
                Information($"Starting {i}/{TestLocations.Count}");
                Execute(input);
                Information($"Done with {i}/{TestLocations.Count}");
                i++;
            }
            Information("We made it!");
        }

        protected override object Execute((string from, string to, uint range) input)
        {
            var from = input.from;
            var to = input.to;

            var tdbsNmbs = TransitDb.ReadFrom(Constants.Nmbs, 0);

            var osmGen = new OsmTransferGenerator(RouterDbStaging.RouterDb, input.range).UseCache();
            // osmGen.PreCalculateCache(tdbsNmbs.Latest.StopsDb.GetReader());


            var stopsReader = tdbsNmbs.Latest.StopsDb.GetReader().AddOsmReader();
            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);

            stopsReader.MoveTo(to);
            var toStop = new Stop(stopsReader);

            var defaultRealLifeProfile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new FirstLastMilePolicy(
                    new CrowsFlightTransferGenerator(),
                    osmGen, new[] {fromStop.Id},
                    osmGen, new[] {toStop.Id}
                ),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(8)
            );


            var calculator = tdbsNmbs.SelectProfile(defaultRealLifeProfile)
                .UseOsmLocations()
                .SelectStops(from, to)
                .SelectTimeFrame(Constants.TestDate.AddHours(9), Constants.TestDate.AddHours(14));

            NotNull(calculator.EarliestArrivalJourney());
            NotNull(calculator.LatestDepartureJourney());

            var all = calculator.AllJourneys();
            NotNull(all);
            True(all.Any());

            return null;
        }
    }
}