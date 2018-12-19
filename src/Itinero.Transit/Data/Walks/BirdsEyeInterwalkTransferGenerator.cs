using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data.Walks
{
    /// <summary>
    /// Generates walks between transport stops, solely based on the distance between them.
    ///
    /// Will generate 
    /// </summary>
    public class BirdsEyeInterwalkTransferGenerator : IOtherModeGenerator
    {
        private readonly StopsDb _stopsDb;
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
        /// <param name="stopsDb"></param>
        /// <param name="maxDistance"></param>
        ///  <param name="speed">In meter per second. According to Wikipedia, about 1.4m/s is preferred average</param>
        public BirdsEyeInterwalkTransferGenerator(
            StopsDb stopsDb, int maxDistance = 500, float speed = 1.4f)
        {
            _stopsDb = stopsDb;
            _reader = stopsDb.GetReader();
            _maxDistance = maxDistance;
            _speed = speed;
        }

        private IEnumerable<IStop> LocationsInRange((uint, uint) source)
        {
            _reader.MoveTo(source);
            var lat = (float) _reader.Latitude;
            var lon = (float) _reader.Longitude;
            var l = new List<(uint, uint)>();

            var box = (
                DistanceEstimate.MoveEast(lat, lon, -_maxDistance), // minLon
                DistanceEstimate.MoveNorth(lat, lon, +_maxDistance), // MinLat
                DistanceEstimate.MoveEast(lat, lon, +_maxDistance), // MaxLon
                DistanceEstimate.MoveNorth(lat, lon, -_maxDistance) //maxLat
            );
            return _stopsDb.SearchInBox(box);
        }

        private float CalculateDistance
            ((uint, uint) departureLocation, (uint, uint) targetLocation)
        {
            _reader.MoveTo(departureLocation);
            var lat0 = (float) _reader.Latitude;
            var lon0 = (float) _reader.Longitude;

            _reader.MoveTo(targetLocation);
            var lat1 = (float) _reader.Latitude;
            var lon1 = (float) _reader.Longitude;

            var distance = DistanceEstimate.DistanceEstimateInMeter(
                lat0, lon0, lat1, lon1);
            return distance;
        }

        /// <summary>
        /// Adds a walk to 'targetLocation' at the end of the journey 'buildOn'
        /// </summary>
        /// <param name="buildOn"></param>
        /// <param name="targetLocation"></param>
        /// <returns></returns>
        public Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, ulong timeWhenLeaving,
            (uint, uint) otherLocation) where T : IJourneyStats<T>
        {
            var distance = CalculateDistance(buildOn.Location, otherLocation);
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
            var distance = CalculateDistance(buildOn.Location, otherLocation);
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
    }
}