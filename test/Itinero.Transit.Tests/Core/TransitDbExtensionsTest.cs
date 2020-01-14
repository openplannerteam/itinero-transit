using System;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Xunit;

namespace Itinero.Transit.Tests.Core
{
    public static class TransitDbExtensionsTest
    {
        [Fact]
        public static void PruneFamilies_ThreeJourneys_TwoFamilies()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var journeys = new List<Journey<TransferMetric>>
            {
                new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 0),
                        new Connection("a", stop0, stop1, 1000, 1000, new TripId(0, 0))
                    ),
                new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 1),
                        new Connection("b", stop0, stop1, 1000, 1000, new TripId(0, 2))
                    ),
                new Journey<TransferMetric>(stop0, 2000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 2),
                        new Connection("c", stop0, stop1, 2000, 1000, new TripId(0, 5))
                    ),
            };
            var families = journeys.PruneFamilies(new ConnectionIdMinimizer());

            Assert.Equal(2, families.Count);
            Assert.Equal((uint) 0, families[0].Connection.LocalId);
            Assert.Equal((uint) 2, families[1].Connection.LocalId);

            families = journeys.PruneFamilies(new ConnectionIdMaximizer());

            Assert.Equal(2, families.Count);
            Assert.Equal((uint) 1, families[0].Connection.LocalId);
            Assert.Equal((uint) 2, families[1].Connection.LocalId);
        }

        [Fact]
        public static void SplitFamilies_ThreeJourneys_TwoFamilies()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var journeys = new List<Journey<TransferMetric>>
            {
                new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 0),
                        new Connection("a", stop0, stop1, 1000, 1000, new TripId(0, 0))
                    ),
                new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 1),
                        new Connection("b", stop0, stop1, 1000, 1000, new TripId(0, 2))
                    ),
                new Journey<TransferMetric>(stop0, 2000, TransferMetric.Factory)
                    .ChainForward(new ConnectionId(0, 0),
                        new Connection("c", stop0, stop1, 2000, 1000, new TripId(0, 5))
                    ),
            };
            var families = journeys.PartitionFamilies();

            // One family departing at 2000...
            Assert.Single(families[2000]);
            // ... with a single member
            Assert.Single(families[2000][0]);

            // One family departing at 1000...
            Assert.Single(families[1000]);
            // Which has TWO members
            Assert.Equal(2, families[1000][0].Count);
        }
    }

    public class ConnectionIdMaximizer : IComparer<Journey<TransferMetric>>
    {
        public int Compare(Journey<TransferMetric> x, Journey<TransferMetric> y)
        {
            if (x == null || y == null)
            {
                throw new NullReferenceException();
            }

            if (x.Connection.LocalId < y.Connection.LocalId)
            {
                return -1;
            }

            return 1;
        }
    }

    public class ConnectionIdMinimizer : IComparer<Journey<TransferMetric>>
    {
        public int Compare(Journey<TransferMetric> x, Journey<TransferMetric> y)
        {
            if (x.Connection.LocalId < y.Connection.LocalId)
            {
                return 1;
            }

            return -1;
        }
    }
}