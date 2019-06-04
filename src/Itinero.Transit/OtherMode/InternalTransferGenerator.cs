using Itinero.Transit.Data;

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