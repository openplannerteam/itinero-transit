using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    // Test with the bicycle profile: from start, to end, with tdb given + OSM reader
    // test EAS, LAS and PCS return something

    public class
        IntermodalTestWithOtherTransport : FunctionalTestWithInput<(string start, string destination, uint maxDistance)>
    {
        private readonly TransitDb _tdb;
        private readonly Func<uint, Stop, Stop, Profile<TransferMetric>> _profile;

        public IntermodalTestWithOtherTransport(
            TransitDb tdb,
           Func<uint, Stop, Stop, Profile<TransferMetric>> profile)
        {
            _tdb = tdb;
            _profile = profile;
        }

        protected override void Execute()
        {
            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates

          
            
            var profile = _profile(Input.maxDistance, null, null);
            var calculator =
                _tdb.SelectProfile(profile)
                .UseOsmLocations()
                .SelectStops(
                    Input.start,
                    Input.destination)
                .SelectTimeFrame(StringConstants.TestDate, StringConstants.TestDate.AddHours(10));

            var start = DateTime.Now;
            NotNull(calculator.LatestDepartureJourney());
            calculator.ResetFilter();
            var end = DateTime.Now;
            Information($"Calculating LAS took {(end - start).TotalMilliseconds}ms");

            start = DateTime.Now;
            var easJ = calculator.EarliestArrivalJourney();
            NotNull(easJ);
            end = DateTime.Now;
            Information($"Calculating EAS took {(end - start).TotalMilliseconds}ms");
            start = DateTime.Now;
            var journeys = calculator.AllJourneys();
            NotNull(journeys);
            True(journeys.Count > 0);
            end = DateTime.Now;
            Information($"Calculating PCS took {(end - start).TotalMilliseconds}ms");
        }
    }
}