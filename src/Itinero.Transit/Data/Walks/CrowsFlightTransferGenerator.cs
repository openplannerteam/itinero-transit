using System;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data.Walks
{
    /// <summary>
    /// Generates walks between transport stops, solely based on the distance between them.
    ///
    /// Will generate 
    /// </summary>
    public class CrowsFlightTransferGenerator : IOtherModeGenerator
    {
        private readonly StopsDb.StopsDbReader _reader;
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
        /// <param name="reader"></param>
        /// <param name="maxDistance">The maximum walkable distance in meter</param>
        ///  <param name="speed">In meter per second. According to Wikipedia, about 1.4m/s is preferred average</param>
        public CrowsFlightTransferGenerator(
            TransitDb transitDb, int maxDistance = 500, float speed = 1.4f)
        {
            _reader = transitDb.Latest.StopsDb.GetReader();
            _maxDistance = maxDistance;
            _speed = speed;
        }

        /// <summary>
        /// Adds a walk to 'targetLocation' at the end of the journey 'buildOn'
        /// </summary>
        /// <param name="buildOn"></param>
        /// <param name="timeWhenLeaving"></param>
        /// <param name="otherLocation"></param>
        /// <returns></returns>
        public Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, ulong timeWhenLeaving,
            (uint, uint) otherLocation) where T : IJourneyStats<T>
        {
            var distance = _reader.CalculateDistanceBetween(buildOn.Location, otherLocation);
            if (distance > _maxDistance)
            {
                return null;
            }

            var walkingTimeInSec = distance * _speed;
            var arrivalTime = buildOn.Time + walkingTimeInSec;

            if (arrivalTime > timeWhenLeaving)
            {
                return null;
            }

            return buildOn.ChainSpecial(
                Journey<T>.WALK, (ulong) arrivalTime, otherLocation, UInt32.MaxValue);
        }


        public Journey<T> CreateArrivingTransfer<T>(Journey<T> buildOn, ulong timeWhenDeparting,
            (uint, uint) otherLocation) where T : IJourneyStats<T>
        {
            var distance = _reader.CalculateDistanceBetween(buildOn.Location, otherLocation);
            if (distance > _maxDistance)
            {
                return null;
            }

            var walkingTimeInSec = distance * _speed;
            var arrivalTime = buildOn.Time - walkingTimeInSec;

            if (arrivalTime < timeWhenDeparting)
            {
                return null;
            }

            return buildOn.ChainSpecial(
                Journey<T>.WALK, (ulong) arrivalTime, otherLocation, UInt32.MaxValue);
        }

        public float Range()
        {
            return _maxDistance;
        }
    }
}