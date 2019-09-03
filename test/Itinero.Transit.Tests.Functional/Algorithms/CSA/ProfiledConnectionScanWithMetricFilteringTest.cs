using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanWithMetricFilteringTest :FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        private void AreSame(ICollection<Journey<TransferMetric>> js, ICollection<Journey<TransferMetric>> bs)
        {
            foreach (var a in js)
            {
                if (!bs.Contains(a))
                {
                    Logging.Log.Error($"Journey {a.ToString(100)} is missing");
                    True(false);
                }
            }

            foreach (var b in bs)
            {
                if (!js.Contains(b))
                {
                    Logging.Log.Error($"Journey {b.ToString(100)} is missing");
                    True(false);
                }
            }
        }

        protected override void Execute()
        {
            Input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6

            var start = DateTime.Now;
            var pcs = new ProfiledConnectionScan<TransferMetric>(Input.GetScanSettings());
            var journeys = pcs.CalculateJourneys();
            var end = DateTime.Now;
            var noFilterTime = (end - start).TotalMilliseconds;
            // verify result.
            NotNull(journeys);

            True(journeys.Any());

            Information($"Found {journeys.Count} profiles without filter in {noFilterTime}ms");


            var settings = Input.GetScanSettings();
            start = DateTime.Now;
            settings.MetricGuesser = new SimpleMetricGuesser<TransferMetric>(
                settings.ConnectionsEnumerator, settings.DepartureStop[0].Item1);
            var pcsF = new ProfiledConnectionScan<TransferMetric>(settings);
            var journeysF = pcsF.CalculateJourneys();
            end = DateTime.Now;
            var filteredTime = (end - start).TotalMilliseconds;
            // verify result.
            NotNull(journeysF);
            True(journeysF.Any());
            Information(
                $"Found {journeysF.Count} profiles in {filteredTime}ms with metric (difference: {noFilterTime - filteredTime}ms faster)");

            AreSame(journeysF, journeys);


            settings = Input.GetScanSettings();
            start = DateTime.Now;
            settings.MetricGuesser = new SimpleMetricGuesser<TransferMetric>(
                settings.ConnectionsEnumerator, settings.DepartureStop[0].Item1);
            var pcsFEarliest = new ProfiledConnectionScan<TransferMetric>(settings);
            var journeysFEarliest = pcsFEarliest.CalculateJourneys();
            end = DateTime.Now;
            var filteredTimeEarliest = (end - start).TotalMilliseconds;
            // verify result.
            NotNull(journeysFEarliest);
            True(journeysFEarliest.Any());
            True(Equals(journeysFEarliest.Count, journeys.Count));

            Information(
                $"Found {journeysFEarliest.Count} profiles in {filteredTimeEarliest}ms with earliest (difference: {noFilterTime - filteredTimeEarliest}ms faster then no filter at all)");
            AreSame(journeysFEarliest, journeys);
        }
    }
}