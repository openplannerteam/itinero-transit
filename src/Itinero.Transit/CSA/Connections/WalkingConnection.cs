using System;

namespace Itinero.Transit
{
    /// <summary>
    /// A walking connection is a connection where the traveller takes walks (or bikes)...
    /// from one location to another.
    ///
    /// Note that a 'WalkingConnection' might also be used to start or end a journey
    /// </summary>
    public class WalkingConnection : IContinuousConnection
    {
        private readonly Uri _arrivalLocation, _departureLocation;
        private readonly DateTime _arrivalTime, _departureTime;
        private readonly Route _route;
        private readonly float _speed;

        /// <summary>
        /// Constructor used to bootstrap a journey, e.g. for code that assumes that the
        /// traveller simply appears at this location
        /// </summary>
        /// <param name="genesisLocation"></param>
        /// <param name="genesisTime"></param>
        public WalkingConnection(Uri genesisLocation, DateTime genesisTime)
        {
            _arrivalLocation = genesisLocation;
            _departureLocation = genesisLocation;

            _arrivalTime = genesisTime;
            _departureTime = genesisTime;
            _route = null;
        }

        public WalkingConnection(Route route, Uri departureLocation, Uri arrivalLocation, DateTime departureTime,
            float speed)
        {
            _route = route;
            _departureLocation = departureLocation;
            _arrivalLocation = arrivalLocation;

            _departureTime = departureTime;
            _arrivalTime = departureTime.AddSeconds(route.TotalDistance * speed);
            _speed = speed;
        }


        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(ILocationProvider locDecode)
        {
            return _route == null
                ? $"Genesis connection at {locDecode.GetNameOf(_departureLocation)} {_departureTime}"
                : $"Walk from {locDecode.GetNameOf(_departureLocation)} to {locDecode.GetNameOf(_arrivalLocation)}, " +
                  $"{_departureTime:HH:mm:ss} --> {_arrivalTime:HH:mm:ss} ({_route.TotalTime}sec, {_route.TotalDistance}m)";
        }

        public Route Walk()
        {
            return _route;
        }

        public IContinuousConnection MoveTime(double seconds)
        {
            return new WalkingConnection(_route, _departureLocation, _arrivalLocation,
                _departureTime.AddSeconds(seconds),
                _speed);
        }

        public IContinuousConnection MoveDepartureTime(DateTime newDepartureTime)
        {
            return new WalkingConnection(_route, _departureLocation, _arrivalLocation, newDepartureTime, _speed);
        }

        public Uri DepartureLocation()
        {
            return _departureLocation;
        }

        public Uri ArrivalLocation()
        {
            return _arrivalLocation;
        }

        public DateTime ArrivalTime()
        {
            return _arrivalTime;
        }

        public DateTime DepartureTime()
        {
            return _departureTime;
        }

        public Uri Operator()
        {
            return null;
        }

        public string Mode()
        {
            return "Walking";
        }

        public Uri Id()
        {
            return null;
        }

        public Uri Trip()
        {
            return null;
        }

        public Uri Route()
        {
            return null;
        }

        public Route AsRoute(ILocationProvider locationProv)
        {
            return _route;
        }


        protected bool Equals(WalkingConnection other)
        {
            return Equals(_arrivalLocation, other._arrivalLocation)
                   && Equals(_departureLocation, other._departureLocation)
                   && Equals(_arrivalTime, other._arrivalTime)
                   && Equals(_departureTime, other._departureTime)
                   && Equals(_route.TotalDistance, other._route.TotalDistance)
                   && Equals(_speed, other._speed);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((WalkingConnection) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_arrivalLocation != null ? _arrivalLocation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_departureLocation != null ? _departureLocation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _arrivalTime.GetHashCode();
                hashCode = (hashCode * 397) ^ _departureTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (_route != null ? _route.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _speed.GetHashCode();
                return hashCode;
            }
        }
    }
}