using System;

// ReSharper disable BuiltInTypeReferenceStyle

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.Journeys
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
            new TransferStats(0, 0, 0);


        // ---------------- ACTUAL STATISTICS -------------------------

        public readonly uint NumberOfTransfers;

        public readonly TimeSpan TravelTime;

        public readonly float WalkingTime;

        private TransferStats(uint numberOfTransfers,
            TimeSpan travelTime,
            float walkingDistance)
        {
            NumberOfTransfers = numberOfTransfers;
            TravelTime = travelTime;
            WalkingTime = walkingDistance;
        }

        public TransferStats EmptyStat()
        {
            return Factory;
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


            ulong walkingTime = 0;
            if (journey.SpecialConnection && journey.Connection == Journey<TransferStats>.WALK)
            {
                walkingTime = travelTime;
            }

            return new TransferStats((uint) (NumberOfTransfers + (transferred ? 1 : 0)),
                (uint) (TravelTime + travelTime),
                WalkingTime + walkingTime);
        }

        public override string ToString()
        {
            var seconds = TravelTime == uint.MaxValue ? 0 : TravelTime;
            var hours = TravelTime / (60 * 60);
            seconds = seconds % (60 * 60);
            var minutes = seconds / 60;
            seconds = seconds % 60;

            return
                $"Stats: {NumberOfTransfers} transfers, {hours}:{minutes}:{seconds} total time), {WalkingTime} seconds to walk";
        }

        private bool Equals(TransferStats other)
        {
            return NumberOfTransfers == other.NumberOfTransfers
                   && TravelTime == other.TravelTime
                   && WalkingTime.Equals(other.WalkingTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TransferStats) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) NumberOfTransfers;
                hashCode = (hashCode * 397) ^ (int) TravelTime;
                hashCode = (hashCode * 397) ^ WalkingTime.GetHashCode();
                return hashCode;
            }
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

    /// <summary>
    /// Compares two BACKWARDS journeys with each other
    /// </summary>
    public class ProfileTransferCompare : ProfiledStatsComparator<TransferStats>
    {
        public override int ADominatesB(Journey<TransferStats> a, Journey<TransferStats> b)
        {
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
                   || a.Root.Time < b.Root.Time
                   || a.Time > b.Time;
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