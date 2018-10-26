using System;
using Itinero;
using Itinero_Transit.CSA.LocationProviders;

namespace Itinero_Transit.CSA.ConnectionProviders
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

        public WalkingConnection(Route route, Uri departureLocation, Uri arrivalLocation, DateTime departureTime, float speed)
        {
            _route = route;
            _departureLocation = departureLocation;
            _arrivalLocation = arrivalLocation;

            _departureTime = departureTime;
            _arrivalTime = departureTime.AddSeconds(route.TotalDistance * speed);
        }
        
        
        public WalkingConnection(Route route, Uri departureLocation, Uri arrivalLocation, DateTime departureTime, DateTime arrivalTime)
        {
            _route = route;
            _departureLocation = departureLocation;
            _arrivalLocation = arrivalLocation;

            _departureTime = departureTime;
            _arrivalTime = arrivalTime;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(ILocationProvider locDecode)
        {
            return _route == null ?
                $"Genesis connection at {locDecode.GetNameOf(_departureLocation)} {_departureTime}" : 
                $"Walk from {locDecode.GetNameOf(_departureLocation)} to {locDecode.GetNameOf(_arrivalLocation)}, this takes {_route.TotalTime}sec ({_route.TotalDistance}m)\n" +
                $"Timing to walk {_departureTime:HH:mm:ss} --> {_arrivalTime:HH:mm:ss}";
        }

        public Route Walk()
        {
            return _route;
        }

        public IContinuousConnection MoveTime(double seconds)
        {
            return new WalkingConnection(_route, _departureLocation, _arrivalLocation, _departureTime.AddSeconds(seconds), _arrivalTime.AddSeconds(seconds));
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

        public bool Continuous()
        {
            return true;
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
    }
}