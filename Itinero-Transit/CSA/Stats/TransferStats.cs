using System;

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// A simple statistic keeping track of the number of trains taken and the total travel time.
    /// This class uses Pareto Optimization. Use either TotalTimeMinimizer or TotalTransferMinimizer to optimize for one of those
    /// </summary>
    public class TransferStats : IJourneyStats
    {
        public readonly int NumberOfTransfers;
        public readonly DateTime StartTime;
        public readonly DateTime EndTime;
        public readonly TimeSpan TravelTime;

        public static readonly MinimizeTransfers MinimizeTransfers = new MinimizeTransfers();
        public static readonly MinimizeTravelTimes MinimizeTravelTimes = new MinimizeTravelTimes();

        public static readonly ProfileCompare ProfileCompare = new ProfileCompare();
        public static readonly ParetoCompare ParetoCompare = new ParetoCompare();

        public static readonly ChainedComparator<TransferStats> MinimizeTransfersFirst =
            new ChainedComparator<TransferStats>(MinimizeTransfers, MinimizeTravelTimes);

        public static readonly ChainedComparator<TransferStats> MinimizeTravelTimeFirst =
            new ChainedComparator<TransferStats>(MinimizeTravelTimes, MinimizeTransfers);


        public static readonly TransferStats Factory =
            new TransferStats(int.MaxValue, DateTime.MinValue, DateTime.MaxValue);

        public TransferStats(int numberOfTransfers, DateTime startTime, DateTime endTime)
        {
            NumberOfTransfers = numberOfTransfers;
            StartTime = startTime;
            EndTime = endTime;
            TravelTime = endTime - startTime;
            if (endTime < startTime)
            {
                throw new ArgumentException("Arrivaltime before departuretime");
            }
        }

        public IJourneyStats InitialStats(IConnection c)
        {
            return new TransferStats(0, c.DepartureTime(), c.ArrivalTime());
        }

        public IJourneyStats Add(Journey journey)
        {
            var transferred =
                journey.Connection.Trip() != null &&
                !journey.Connection.Trip().Equals(journey.PreviousLink.Connection.Trip());


            var dep = journey.Connection.DepartureTime();
            if (StartTime < dep)
            {
                dep = StartTime;
            }

            var arr = journey.Connection.ArrivalTime();
            if (EndTime > arr)
            {
                arr = EndTime;
            }

            return new TransferStats(NumberOfTransfers + (transferred ? 1 : 0), dep, arr);
        }

        public override bool Equals(object obj)
        {
            if (obj is TransferStats other)
            {
                return Equals(other);
            }

            return false;
        }

        internal bool Equals(TransferStats other)
        {
            return NumberOfTransfers == other.NumberOfTransfers &&
                   (EndTime - StartTime).Equals(other.EndTime - other.StartTime);
        }

        public override int GetHashCode()
        {
            return NumberOfTransfers + (EndTime - StartTime).GetHashCode();
        }

        public override string ToString()
        {
            return $"{NumberOfTransfers} transfers, {EndTime - StartTime}";
        }
    }


    public class MinimizeTransfers : IStatsComparator<TransferStats>
    {
        public int ADominatesB(TransferStats a, TransferStats b)
        {
            return a.NumberOfTransfers.CompareTo(b.NumberOfTransfers);
        }
    }

    public class MinimizeTravelTimes : IStatsComparator<TransferStats>
    {
        public int ADominatesB(TransferStats a, TransferStats b)
        {
            return (a.EndTime - a.StartTime).CompareTo(b.EndTime - b.StartTime);
        }
    }

    public class ProfileCompare : IStatsComparator<TransferStats>
    {
        public int ADominatesB(TransferStats a, TransferStats b)
        {
            if (a.Equals(b))
            {
                return 0;
            }

            if (S1DominatesS2(a, b))
            {
                return -1;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (S1DominatesS2(b, a))
            {
                return 1;
            }

            return int.MaxValue;
        }

        private bool S1DominatesS2(TransferStats s1, TransferStats s2)
        {
            return
                s1.NumberOfTransfers <= s2.NumberOfTransfers
                && s1.StartTime >= s2.StartTime
                && s1.EndTime <= s2.EndTime;
        }
    }

    public class ParetoCompare : IStatsComparator<TransferStats>
    {
        public int ADominatesB(TransferStats a, TransferStats b)
        {
            if (a.Equals(b))
            {
                return 0;
            }

            if (S1DominatesS2(a, b))
            {
                return -1;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (S1DominatesS2(b, a))
            {
                return 1;
            }

            return int.MaxValue;
        }

        private bool S1DominatesS2(TransferStats s1, TransferStats s2)
        {
            return
                s1.NumberOfTransfers < s2.NumberOfTransfers
                && s1.TravelTime < s2.TravelTime;
        }
    }
}