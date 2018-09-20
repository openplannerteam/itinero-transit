using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A simple statistic keeping track of the number of trains taken and which tries to keep the number of transfers as low as possible.
    /// </summary>
    public class TransferStats : IJourneyStats
    {
        private readonly int _numberOfTransfers;

        public TransferStats(int numberOfTransfers)
        {
            _numberOfTransfers = numberOfTransfers;
        }

        public IJourneyStats EmptyStats()
        {
            return new TransferStats(0);
        }

        public IJourneyStats Add(Journey j)
        {
            if (j.Connection.GtfsTrip.Equals(j.PreviousLink.Connection.GtfsTrip))
            {
                // We didn't transfer; no extra transfer cost
                return this;
            }

            return new TransferStats(_numberOfTransfers + 1);
        }

        public bool IsComparableTo(IJourneyStats stats)
        {
            return stats is TransferStats;
        }
        
        public int CompareTo(object obj)
        {
            if (obj is TransferStats item)
            {
                return _numberOfTransfers.CompareTo(item._numberOfTransfers);
            }

            throw new ArgumentException("Equals of a Transferstat with incorrect object");
        }

        public override bool Equals(object obj)
        {
            if (obj is TransferStats item)
            {
                return _numberOfTransfers == item._numberOfTransfers;
            }

            return false;
        }

        protected bool Equals(TransferStats other)
        {
            return _numberOfTransfers == other._numberOfTransfers;
        }

        public override int GetHashCode()
        {
            return _numberOfTransfers;
        }

    }
}