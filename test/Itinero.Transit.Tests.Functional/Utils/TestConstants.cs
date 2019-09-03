using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;

namespace Itinero.Transit.Tests.Functional.Utils
{
    /// <summary>
    /// This class contains all Algorithmic tests + all inputs for them
    /// </summary>
    public static class TestConstants
    {
        public static readonly (string, int) PrGentWeba = ("https://www.openstreetmap.org/relation/9508548", 5);

        public static readonly (string, int) PrGentWatersport =
            ("https://www.openstreetmap.org/relation/9594575?xhr=1&map=6513", 3);

        public static readonly (string, int) ShuttleBrugge = ("9413958", 10);

        public static List<(string, int)> OsmRelationsToTest = new List<(string, int)>
        {
            PrGentWeba,
            PrGentWatersport,
            ShuttleBrugge
        };

        public static Profile<TransferMetric> DefaultProfile(uint maxSearch)
        {
            return new DefaultProfile(maxSearch);
        }

        public static Profile<TransferMetric> WithWalk(uint maxSearch = 500)
        {
            return new Profile<TransferMetric>(
                new OsmTransferGenerator(RouterDbStaging.RouterDb, maxSearch).UseCache(),
                new InternalTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(8)
            );
        }

        public static Profile<TransferMetric> WithFirstLastMile(IStop firstMile, IStop lastMile)
        {
            var router = RouterDbStaging.RouterDb;
            var gen = new FirstLastMilePolicy(new CrowsFlightTransferGenerator(),
                new OsmTransferGenerator(router),
                firstMile,
                new OsmTransferGenerator(router),
                lastMile).UseCache();

            return new Profile<TransferMetric>(
                gen,
                new InternalTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(8)
            );
        }


        public static List<(string departure, string arrival, uint maxSearchDistance)> WithWalkTestCases =
            new List<(string departure, string arrival, uint maxSearchDistance)>
            {
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.Brugge, 1000),
                (StringConstants.Brugge, StringConstants.Gent, 1000),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.Gent, 1000),
                (StringConstants.Brugge, StringConstants.OsmDeSterre, 2500),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.OsmDeSterre, 5000),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.OsmHermanTeirlinck, 5000),
                (StringConstants.OsmHermanTeirlinck, StringConstants.OsmDeSterre, 5000),
                (StringConstants.OsmWechel, StringConstants.OsmDeSterre, 25000),
            };

        public static readonly List<FunctionalTestWithInput<WithTime<TransferMetric>>> AllAlgorithmicTests =
            new List<FunctionalTestWithInput<WithTime<TransferMetric>>>
            {
                new EarliestConnectionScanTest(),
                new LatestConnectionScanTest(),
                new ProfiledConnectionScanTest(), //*/
                new EasPcsComparison(),
                new EasLasComparison(),
                new IsochroneTest(),
                // TODO Fix this test      new ProfiledConnectionScanWithMetricFilteringTest(),
            };


        /// <summary>
        /// Tests only over NMBs-network
        /// </summary>
        public static List<WithTime<TransferMetric>> NmbsInputs(WithProfile<TransferMetric> withProfile,
            DateTime date)
        {
            withProfile = withProfile.PrecalculateClosestStops();

            return new List<WithTime<TransferMetric>>
            {
                withProfile.SelectStops(
                    StringConstants.Brugge, StringConstants.Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                withProfile.SelectStops(StringConstants.Poperinge, StringConstants.Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(StringConstants.Brugge, StringConstants.Poperinge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(
                    StringConstants.Oostende,
                    StringConstants.Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    StringConstants.Brugge,
                    StringConstants.Oostende).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    StringConstants.BrusselZuid,
                    StringConstants.Leuven).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    StringConstants.Leuven,
                    StringConstants.SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    StringConstants.BrusselZuid,
                    StringConstants.SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    StringConstants.Brugge,
                    StringConstants.Kortrijk).SelectTimeFrame(
                    date.Date.AddHours(10),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(
                    StringConstants.Kortrijk,
                    StringConstants.Vielsalm).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(18)) //*/
            };
        }


        /// <summary>
        /// Test cases over multiple operators.
        /// Requires DeLijn to be part of the TransitDb
        /// </summary>
        public static List<WithTime<TransferMetric>> MultimodalInputs(
            WithProfile<TransferMetric> withProfile, DateTime date)
        {
            withProfile = withProfile.PrecalculateClosestStops();

            return new List<WithTime<TransferMetric>>
            {
                withProfile.SelectStops(StringConstants.CoiseauKaaiOsm,
                    StringConstants.Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(StringConstants.CoiseauKaaiOsm,
                    StringConstants.GentZwijnaardeDeLijn).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
            };
        }
    }
}