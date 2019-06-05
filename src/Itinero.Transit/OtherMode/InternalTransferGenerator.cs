using System.Collections.Generic;
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

        public Dictionary<LocationId, uint> TimesBetween(IStopsReader reader, LocationId @from, IEnumerable<LocationId> to)
        {
            // It is a tad weird to have this method implemented, as this one only works when from == to...
            // But well, here we go anyway
            return this.DefaultTimesBetween(reader, from, to);
        }

        public float Range()
        {
            return 0.0f;
        }
    }
}