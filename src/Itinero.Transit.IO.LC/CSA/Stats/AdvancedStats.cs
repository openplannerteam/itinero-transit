using System;

namespace Itinero.Transit
{
    /// <inheritdoc />
    /// <summary>
    /// Keeps track of as much statistics as possible for showcasing
    /// </summary>
    public class AdvancedStats : IJourneyStats<AdvancedStats>
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

        public AdvancedStats InitialStats(IConnection c)
        {
            return ConnectionStats(c);
        }


        public AdvancedStats ConnectionStats(IConnection c)
        {
            var trainTime = (c.ArrivalTime() - c.DepartureTime()).TotalSeconds;
            return new AdvancedStats()
            {
                NumberOfTransfers = 0,
                TotalTrainTime = trainTime,
                TotalTransferTime = 0,
                MinTransferTime = int.MaxValue,
                MaxTransferTime = 0,
            };
        }

        public AdvancedStats Add(Journey<AdvancedStats> journey)
        {
            var c = journey.Connection;
            var connectionStats = ConnectionStats(c);

            if (c.Trip() != null && c.Trip().Equals(journey.PreviousLink.Connection.Trip()))
            {
                // TODO check transfertime for forward and backward situations
                var transfertime = (c.DepartureTime() - journey.PreviousLink.Connection.ArrivalTime())
                    .TotalSeconds;
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
    }
}