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
    public class FullStackTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            var from = Constants.NearStationBruggeLatLon;
            var to = Constants.Gent;

            var tdbsNmbs = TransitDb.ReadFrom(Constants.Nmbs, 0);

            var osmGen = new OsmTransferGenerator(RouterDbStaging.RouterDb).UseCache();
            // osmGen.PreCalculateCache(tdbsNmbs.Latest.StopsDb.GetReader());

            var stopsReader = (IStopsReader) new OsmLocationStopReader(1);
            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);

            stopsReader = tdbsNmbs.Latest.StopsDb.GetReader();
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