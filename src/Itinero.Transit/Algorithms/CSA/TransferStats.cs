using System;

// ReSharper disable BuiltInTypeReferenceStyle

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit
{
    using TimeSpan = UInt32;
    using Time = UInt32;
    using Id = UInt32;


    /// <inheritdoc />
    /// <summary>
    /// A simple statistic keeping track of the number of trains taken and the total travel time.
    /// This class uses Pareto Optimization. Use either TotalTimeMinimizer or TotalTransferMinimizer to optimize for one of those
    /// </summary>
    public class TransferStats : IJourneyStats<TransferStats>
    {
        //------------------ ALL KINDS OF COMPARATORS -------------------

        public static readonly MinimizeTransfers MinimizeTransfers = new MinimizeTransfers();
        public static readonly MinimizeTravelTimes MinimizeTravelTimes = new MinimizeTravelTimes();

        public static readonly ProfileTransferCompare ProfileTransferCompare = new ProfileTransferCompare();
        public static readonly ProfileCompare ProfileCompare = new ProfileCompare();
        public static readonly ParetoCompare ParetoCompare = new ParetoCompare();

        public static readonly ChainedComparator<TransferStats> MinimizeTransfersFirst =
            new ChainedComparator<TransferStats>(MinimizeTransfers, MinimizeTravelTimes);

        // ReSharper disable once UnusedMember.Global
        public static readonly ChainedComparator<TransferStats> MinimizeTravelTimeFirst =
            new ChainedComparator<TransferStats>(MinimizeTravelTimes, MinimizeTransfers);


        // ----------------- ZERO ELEMENT ------------------

        public static readonly TransferStats Factory =
            new TransferStats(int.MaxValue, TimeSpan.MaxValue, int.MaxValue);


        // ---------------- ACTUAL STATISTICS -------------------------

        public readonly uint NumberOfTransfers;

        public readonly TimeSpan TravelTime;

        public readonly float WalkingDistance;


        private TransferStats(uint numberOfTransfers,
            TimeSpan travelTime,
            float walkingDistance)
        {
            NumberOfTransfers = numberOfTransfers;
            TravelTime = travelTime;
            WalkingDistance = walkingDistance;
        }

        public TransferStats() : this(0, 0, 0)
        {
        }

        public TransferStats EmptyStat()
        {
            return new TransferStats();
        }

        public TransferStats Add(Journey<TransferStats> journey)
        {
            var transferred = journey.PreviousLink.LastTripId() != journey.LastTripId()
                              && !(journey.PreviousLink.SpecialConnection &&
                                   journey.PreviousLink.Connection == Journey<TransferStats>.GENESIS);

            ulong travelTime;

            if (journey.Time > journey.PreviousLink.Time)
            {
                travelTime = journey.Time - journey.PreviousLink.Time;
            }
            else
            {
                travelTime = journey.PreviousLink.Time - journey.Time;
            }

            return new TransferStats((uint) (NumberOfTransfers + (transferred ? 1 : 0)),
                (uint) (TravelTime + travelTime),
                WalkingDistance + 0);
        }

        public override string ToString()
        {
            return
                $"Stats: {NumberOfTransfers} transfers, {TravelTime} total time), {WalkingDistance}m to walk";
        }
    }


    public class MinimizeTransfers : StatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            return a.Stats.NumberOfTransfers.CompareTo(b.Stats.NumberOfTransfers);
        }
    }

    public class MinimizeTravelTimes : StatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            return (a.Stats.TravelTime).CompareTo(b.Stats.TravelTime);
        }
    }

    public class ProfileTransferCompare : ProfiledStatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            if (a.Equals(b))
            {
                return 0;
            }

            var aBetterThenB = AIsBetterThenB(a, b);
            var bBetterThenA = AIsBetterThenB(b, a);

            if (aBetterThenB && bBetterThenA)
            {
                // No Domination either way
                return int.MaxValue;
            }

            if (aBetterThenB)
            {
                return -1;
            }

            if (bBetterThenA)
            {
                return 1;
            }

            // both perform the same
            return 0;
        }

        /// <summary>
        /// Returns true if A performs better then B in at least one aspect.
        /// This does imply that A dominates B!
        /// This does only imply that B does _not_ dominate A
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool AIsBetterThenB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            return a.Stats.NumberOfTransfers < b.Stats.NumberOfTransfers
                   || a.PreviousLink.Time > b.PreviousLink.Time
                   || a.Time < b.Time;
        }
    }


    public class ProfileCompare : ProfiledStatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            if (a.Equals(b))
            {
                return 0;
            }

            var aBetterThenB = AIsBetterThenB(a, b);
            var bBetterThenA = AIsBetterThenB(b, a);

            if (aBetterThenB && bBetterThenA)
            {
                // No Domination either way
                return int.MaxValue;
            }

            if (aBetterThenB)
            {
                return -1;
            }

            if (bBetterThenA)
            {
                return 1;
            }

            // both perform the same
            return 0;
        }

        /// <summary>
        /// Returns true if A performs better then B in at least one aspect.
        /// This does imply that A dominates B!
        /// This does only imply that B does _not_ dominate A
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool AIsBetterThenB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            return a.PreviousLink.Time > b.PreviousLink.Time
                   || a.Time < b.Time;
        }
    }

    public class ParetoCompare : StatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
            if (a.Stats.TravelTime.Equals(b.Stats.TravelTime) &&
                a.Stats.NumberOfTransfers.Equals(b.Stats.NumberOfTransfers))
            {
                return 0;
            }

            if (S1DominatesS2(a.Stats, b.Stats))
            {
                return -1;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (S1DominatesS2(b.Stats, a.Stats))
            {
                return 1;
            }

            return int.MaxValue;
        }

        private bool S1DominatesS2(TransferStats s1, TransferStats s2)
        {
            return
                (s1.NumberOfTransfers < s2.NumberOfTransfers
                 && s1.TravelTime <= s2.TravelTime)
                || (s1.NumberOfTransfers <= s2.NumberOfTransfers
                    && s1.TravelTime < s2.TravelTime);
        }
    }
}