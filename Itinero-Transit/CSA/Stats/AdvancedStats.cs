using System;

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// Keeps track of as much statistics as possible for showcasing
    /// </summary>
    public class AdvancedStats : IJourneyStats
    {
        public int NumberOfTransfers;

        /// <summary>
        /// Time statistics, in seconds
        /// </summary>
        public double TotalTrainTime, TotalTransferTime, MinTransferTime, MaxTransferTime;

        public AdvancedStats()
        {
            NumberOfTransfers = 0;
            TotalTrainTime = 0;
            TotalTransferTime = 0;
            MinTransferTime = 0;
            MaxTransferTime = 0;
        }

        public IJourneyStats InitialStats(Connection c)
        {
            return ConnectionStats(c);
        }


        public AdvancedStats
            ConnectionStats(Connection c)
        {
            var trainTime = (c.ArrivalTime - c.DepartureTime).TotalSeconds + c.ArrivalDelay - c.DepartureDelay;
            return new AdvancedStats()
            {
                NumberOfTransfers = 0,
                TotalTrainTime = trainTime,
                TotalTransferTime = 0,
                MinTransferTime = int.MaxValue,
                MaxTransferTime = 0,
            };
        }

        public IJourneyStats Add(Journey journey)
        {
            var c = journey.Connection;
            var connectionStats = ConnectionStats(c);

            if (c.GtfsTrip.Equals(journey.PreviousLink.Connection.GtfsTrip))
            {
                var transfertime = (c.DepartureTime - journey.PreviousLink.Time)
                                   .TotalSeconds
                                   + c.DepartureDelay - journey.PreviousLink.Connection.ArrivalDelay;
                return new AdvancedStats()
                {
                    NumberOfTransfers = NumberOfTransfers + 1, //
                    TotalTrainTime = TotalTrainTime + connectionStats.TotalTrainTime,
                    TotalTransferTime = TotalTransferTime + transfertime,
                    MinTransferTime = Math.Min(transfertime, MinTransferTime),
                    MaxTransferTime = Math.Max(MaxTransferTime, transfertime),
                };
            }
            else
            {
                return new AdvancedStats()
                {
                    NumberOfTransfers = NumberOfTransfers + 0, //
                    TotalTrainTime = TotalTrainTime + connectionStats.TotalTrainTime,
                    TotalTransferTime = TotalTransferTime,
                    MinTransferTime = MinTransferTime,
                    MaxTransferTime = MaxTransferTime,
                };
            }
        }


        public override string ToString()
        {
            return
                $"numberOfTransfers {NumberOfTransfers}\ntotalTrainTime {TotalTrainTime}\ntotalTransferTime {TotalTransferTime}" +
                $"\nminTransferTime {MinTransferTime}\nmaxTransferTime {MaxTransferTime}";
        }

        public int CompareTo(object obj)
        {
            throw new System.NotImplementedException();
        }

        public bool IsComparableTo(IJourneyStats stats)
        {
            throw new NotImplementedException();
        }
    }
}