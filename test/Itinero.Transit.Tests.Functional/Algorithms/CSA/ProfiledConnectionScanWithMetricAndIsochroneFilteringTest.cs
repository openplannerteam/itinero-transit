using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanWithMetricAndIsochroneFilteringTest : FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        
        protected override void Execute()
        {
            Input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6


            // Make sure that the walks are cached
            Input.ResetFilter();
            var pcs0 = new ProfiledConnectionScan<TransferMetric>(Input.GetScanSettings());
            var journeys0 = pcs0.CalculateJourneys();

            
            Input.ResetFilter();
            var start = DateTime.Now;
            var pcs = new ProfiledConnectionScan<TransferMetric>(Input.GetScanSettings());
            var journeys = pcs.CalculateJourneys();
            var end = DateTime.Now;
            var noFilterTime = (end - start).TotalMilliseconds;
            // verify result.
            NotNull(journeys);

            True(journeys.Any());

            Information($"Found {journeys.Count} profiles without filter in {noFilterTime}ms");


            Input.ResetFilter();
            var settings = Input.GetScanSettings();
            start = DateTime.Now;
            
            Input.IsochroneFrom();
            settings.MetricGuesser = new SimpleMetricGuesser<TransferMetric>(
                settings.ConnectionsEnumerator, settings.DepartureStop[0]);
            var pcsF = new ProfiledConnectionScan<TransferMetric>(settings);
            
            var journeysF = pcsF.CalculateJourneys();
            end = DateTime.Now;
            var filteredTime = (end - start).TotalMilliseconds;
            // verify result.
            Information($"Found {journeysF.Count} profiles");
            Information($"No filter: {noFilterTime}ms, with filter: {filteredTime}ms, diff {noFilterTime - filteredTime}ms faster, {(int) (100*filteredTime/noFilterTime)}% of original)");

            AssertAreSame(journeysF, journeys, Input.StopsReader);
        }
    }
}