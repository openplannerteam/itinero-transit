using Itinero.Transit.Data;
using Itinero.Transit.Journey;

namespace Itinero.Transit.OtherMode
{
    /// <summary>
    ///  Generates internal (thus within the station) transfers if there is enough time to make the transfer.
    /// Returns null if two different locations are given
    /// </summary>
    public class InternalTransferGenerator : IOtherModeGenerator
    {
        private readonly uint _internalTransferTime;

        public InternalTransferGenerator(uint internalTransferTime = 180)
        {
            _internalTransferTime = internalTransferTime;
        }


        public Journey<T> CreateDepartureTransfer<T>(IStopsReader _, Journey<T> buildOn,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!otherLocation.Equals(buildOn.Location))
            {
                // Internal transfer policy does not take care of different locations
                return null;
            }

            return buildOn.Transfer(buildOn.Time + _internalTransferTime);
        }

        public Journey<T> CreateArrivingTransfer<T>(IStopsReader _, Journey<T> buildOn,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!otherLocation.Equals(buildOn.Location))
            {
                return null;
            }

            return buildOn.Transfer(buildOn.Time - _internalTransferTime);
        }

        public uint TimeBetween(IStopsReader _, LocationId from, LocationId to)
        {
            return !from.Equals(to) ? 
                uint.MaxValue : 
                _internalTransferTime;
        }

        public float Range()
        {
            return 0.0f;
        }
    }
}