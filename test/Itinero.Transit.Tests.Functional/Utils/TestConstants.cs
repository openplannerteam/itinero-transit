using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Transfers;

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

        public static Profile<TransferMetric> DefaultProfile(uint maxSearch, StopId _, StopId __)
        {
            return new DefaultProfile(maxSearch);
        }

        public static Profile<TransferMetric> WithWalk(uint maxSearch = 500)
        {
            return new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new OsmTransferGenerator(RouterDbStaging.RouterDb, maxSearch).UseCache(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(8)
            );
        }

        public static Profile<TransferMetric> WithFirstLastMile(uint maxSearchDistance, StopId firstMile,
            StopId lastMile)
        {
            var router = RouterDbStaging.RouterDb;
            IOtherModeGenerator gen = new CrowsFlightTransferGenerator(maxSearchDistance);
            gen = new FirstLastMilePolicy(gen,
                new OsmTransferGenerator(router, maxSearchDistance),
                firstMile,
                new OsmTransferGenerator(router, maxSearchDistance),
                lastMile).UseCache();

            return new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                gen,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(8)
            );
        }


        /// <summary>
        /// Test cases where the expected journey is to _not_ take public transport at all
        /// </summary>
        public static List<(string departure, string arrival, uint maxDistance)> WithDirectWalkTestCases =
            new List<(string departure, string arrival, uint maxDistance)>
            {
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.Brugge, 10000),
            };


        /// <summary>
        /// Test cases where one has to take public transport, possibly with an obligatory walk before- or afterwards
        /// </summary>
        public static readonly List<(string departure, string arrival, uint maxDistance)> WithWalkAndPtTestCases =
            new List<(string departure, string arrival, uint maxDistance)>
            {
                ("http://irail.be/stations/NMBS/008811262", "http://irail.be/stations/NMBS/008811197", 0),
                (StringConstants.Brugge, StringConstants.Gent, 1000),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.Gent, 1000),
                (StringConstants.Brugge, StringConstants.OsmDeSterre, 5000),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.OsmDeSterre, 5000),
                (StringConstants.OsmNearStationBruggeLatLon, StringConstants.OsmHermanTeirlinck, 5000),
                (StringConstants.OsmHermanTeirlinck, StringConstants.OsmDeSterre, 5000),
                (StringConstants.OsmWechel, StringConstants.Gent, 15000),
                (StringConstants.OsmTielen, StringConstants.OsmHerentals, 10000),
                (StringConstants.Aywaille, StringConstants.Florenville, 0)
            };


        /// <summary>
        /// A big pile of test cases
        /// </summary>
        public static List<(string departure, string arrival, uint maxDistance)> WithWalkTestCases =
            WithDirectWalkTestCases.Concat(WithWalkAndPtTestCases).ToList();


        public static List<(string departure, string arrival, uint maxDistance)> OpenHopperTestCases()
        {
            return File.ReadAllLines("testdata/OpenHopperLogsSuccessful.csv")
                .Select(testCase =>
                {
                    var splitted = testCase.Split(",");
                    return (splitted[0], splitted[1], (uint) 25000);
                }).ToList();
        }

        public static readonly List<FunctionalTestWithInput<WithTime<TransferMetric>>> AllAlgorithmicTests =
            new List<FunctionalTestWithInput<WithTime<TransferMetric>>>
            {
                new EarliestConnectionScanTest(),
                new LatestConnectionScanTest(),
                new ProfiledConnectionScanTest(),
                new EasPcsComparison(),
                new EasLasComparison(),
                new IsochroneTest(),
                new ProfiledConnectionScanWithIsochroneFilteringTest(),
                new ProfiledConnectionScanWithMetricFilteringTest(),
                new ProfiledConnectionScanWithMetricAndIsochroneFilteringTest()
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
                    date.Date.AddHours(18))
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
                withProfile.SelectStops(StringConstants.CoiseauKaaiOsmNode,
                    StringConstants.Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(StringConstants.CoiseauKaaiOsmNode,
                    StringConstants.GentZwijnaardeDeLijn).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
            };
        }
    }
}
