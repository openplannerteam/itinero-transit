using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.OtherMode
{
    /// <summary>
    /// Generates walks between transport stops, solely based on the distance between them.
    /// The time needed depends on the given parameter
    ///
    /// </summary>
    public class CrowsFlightTransferGenerator : IOtherModeGenerator
    {
        private readonly int _maxDistance;
        private readonly float _speed;

        ///  <summary>
        ///  Generates a walk constructor.
        /// 
        ///  A walk will only be generated between two locations iff:
        ///  - The given locations are not the same
        ///  - The given locations are no more then 'maxDistance' away from each other.
        /// 
        ///  The time needed for this transfer is calculated based on
        ///  - the distance between the two locations and
        ///  - the speed parameter
        ///  
        ///  </summary>
        /// <param name="maxDistance">The maximum walkable distance in meter</param>
        ///  <param name="speed">In meter per second. According to Wikipedia, about 1.4m/s is preferred average</param>
        public CrowsFlightTransferGenerator(int maxDistance = 500, float speed = 1.4f)
        {
            _maxDistance = maxDistance;
            _speed = speed;
        }


        public uint TimeBetween(IStop from, IStop to)
        {
            var distance =
                DistanceEstimate.DistanceEstimateInMeter(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
            if (distance > _maxDistance)
            {
                return uint.MaxValue;
            }

            return (uint) (distance * _speed);
        }

        public Dictionary<StopId, uint> TimesBetween(IStop @from,
            IEnumerable<IStop> to)
        {
            return this.DefaultTimesBetween(from, to);
        }

        public Dictionary<StopId, uint> TimesBetween(IEnumerable<IStop> @from, IStop to)
        {
            return this.DefaultTimesBetween(from, to);
        }


        public float Range()
        {
            return _maxDistance;
        }

        public string OtherModeIdentifier()
        {
            return $"crowsflight&maxDistance={_maxDistance}&speed={_speed}";
        }

        public IOtherModeGenerator GetSource(StopId @from, StopId to)
        {
            return this;
        }
    }
}