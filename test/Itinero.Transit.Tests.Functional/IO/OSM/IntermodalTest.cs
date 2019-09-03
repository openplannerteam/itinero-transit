using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;
using OsmSharp.IO.PBF;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    // Test with the bicycle profile: from start, to end, with tdb given + OSM reader
    // test EAS, LAS and PCS return something

    public class
        IntermodalTestWithOtherTransport : FunctionalTestWithInput<(string start, string destination, uint
            maxSearchDistance)>
    {
        private readonly WithProfile<TransferMetric> _withProfile;

        public IntermodalTestWithOtherTransport(WithProfile<TransferMetric> withProfile)
        {
            _withProfile = withProfile;
        }

        protected override void Execute()
        {
            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var calculator = _withProfile
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
            True(calculator.AllJourneys().Count > 0);
            end = DateTime.Now;
            Information($"Calculating PCS took {(end - start).TotalMilliseconds}ms");
        }
    }
}