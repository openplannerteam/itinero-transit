using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

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


        public uint TimeBetween(IStop from, IStop to)
        {
            // The distance should be small enough
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (DistanceEstimate.DistanceEstimateInMeter(from.Latitude, from.Longitude, to.Latitude, to.Longitude) >
                Range())
            {
                return uint.MaxValue;
            }

            return
                _internalTransferTime;
        }

        public Dictionary<LocationId, uint> TimesBetween(IStop from,
            IEnumerable<IStop> to)
        {
            // It is a tad weird to have this method implemented, as this one only works when from == to...
            // But well, here we go anyway
            return this.DefaultTimesBetween(from, to);
        }

        public float Range()
        {
            return 1.0f;
        }
    }
}