using Itinero.Transit.Data;
using Itinero.Transit.Journey;

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


        public uint TimeBetween(IStopsReader reader, LocationId from, LocationId to)
        {
            var distance = reader.CalculateDistanceBetween(from, to);
            if (distance > _maxDistance)
            {
                return uint.MaxValue;
            }

            return (uint) (distance * _speed);
        }

        /// <summary>
        /// Adds a walk to 'targetLocation' at the end of the journey 'buildOn'
        /// </summary>
        public Journey<T> CreateDepartureTransfer<T>(IStopsReader reader, Journey<T> buildOn,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {
            var time = TimeBetween(reader, buildOn.Location, otherLocation);
            if (uint.MaxValue == time)
            {
                return null;
            }

            var arrivalTime = buildOn.Time + time;


            return buildOn.ChainSpecial(
                Journey<T>.WALK, arrivalTime, otherLocation, TripId.Walk);
        }


        public Journey<T> CreateArrivingTransfer<T>(IStopsReader reader, Journey<T> buildOn,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {
            var time = TimeBetween(reader, buildOn.Location, otherLocation);
            if (uint.MaxValue == time)
            {
                return null;
            }

            var arrivalTime = buildOn.Time - time;

            return buildOn.ChainSpecial(
                Journey<T>.WALK, arrivalTime, otherLocation, TripId.Walk);
        }

        public float Range()
        {
            return _maxDistance;
        }
    }
}