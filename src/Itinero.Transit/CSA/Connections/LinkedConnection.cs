using System;
using System.Diagnostics.CodeAnalysis;
using Itinero.LocalGeo;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

// ReSharper disable MemberCanBePrivate.Global


namespace Itinero.Transit
{
    /**
     * A connection represents a single connection someone can take.
     * It consists of a departure and arrival stop, departure and arrival time.
     * Note that a connection does _never_ have intermediate stops.
     */
    [Serializable()]
    public class LinkedConnection : LinkedObject, IConnection
    {
        private Uri _departureStop;
        private Uri _arrivalStop;

        private DateTime _departureTime;
        private DateTime _arrivalTime;

        /// <summary>
        /// Human readable name where the vehicle is heading (e.g. "Brugge")
        /// Aka the 'headsign'
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// URI of the current trip sequence, where _all_ the stops can be seen; with delays
        /// </summary>
        public Uri GtfsTrip { get; set; }

        /// <summary>
        /// URI of the route of this train connection. (e.g. the train connection Oostende-Eupen) aspecific of time
        /// Looks very much the same as GTFS-trip (on IRail they are identical)
        /// </summary>
        public Uri GtfsRoute { get; set; }


        // ReSharper disable once UnusedMember.Global
        public LinkedConnection(Uri uri) : base(uri)
        {
        }

        public LinkedConnection(Uri id, Uri departureStop, Uri arrivalStop, DateTime departureTime,
            DateTime arrivalTime) : base(id)
        {
            _departureTime = departureTime;
            _arrivalTime = arrivalTime;
            _arrivalStop = arrivalStop;
            _departureStop = departureStop;
        }

        public LinkedConnection(JObject json) : base(json.GetId())
        {
            FromJson(json);
        }


        public override string ToString()
        {
            return ToString(null);
        }


        public string ToString(ILocationProvider locationDecoder)
        {
            return
                $"Linked Connection {locationDecoder.GetNameOf(_departureStop)} {_departureTime:HH:mm} --> {locationDecoder.GetNameOf(_arrivalStop)} {_arrivalTime:HH:mm}" +
                $"  {Uri}";
        }


        protected sealed override void FromJson(JObject json)
        {
            json.AssertTypeIs("http://semweb.mmlab.be/ns/linkedconnections#Connection");

            _departureStop = json.GetId("http://semweb.mmlab.be/ns/linkedconnections#departureStop");
            _arrivalStop = json.GetId("http://semweb.mmlab.be/ns/linkedconnections#arrivalStop");

            var depDel = json.GetInt("http://semweb.mmlab.be/ns/linkedconnections#departureDelay", 0);
            // Departure time already includes delay
            _departureTime = json.GetDate("http://semweb.mmlab.be/ns/linkedconnections#departureTime");

            var arrDel = json.GetInt("http://semweb.mmlab.be/ns/linkedconnections#arrivalDelay", 0);
            // Arrival time already includes delay
            _arrivalTime = json.GetDate("http://semweb.mmlab.be/ns/linkedconnections#arrivalTime");

            Direction = json.GetLDValue("http://vocab.gtfs.org/terms#headsign");
            GtfsTrip = json.GetId("http://vocab.gtfs.org/terms#trip");
            GtfsRoute = json.GetId("http://vocab.gtfs.org/terms#route");


            if (_arrivalTime <= _departureTime)
            {
                // Sometimes, a departure delay is already known but the arrival delay is not known yet
                // Thus, the arrivalDelay defaults to 0
                // This can lead to (esp. on short connections of only a few minutes) departure times which lie _after_
                // the arrival time
                // We fix this by estimating the arrival delay to be equal to the departureDelay
                depDel += arrDel;
                _arrivalTime = _arrivalTime.AddSeconds(depDel);
            }

            if (_arrivalStop.Equals(_departureStop))
            {
                throw new ArgumentException($"This connection ends where it starts, namely at {_arrivalStop}\n{Id()}");
            }

            if (_arrivalTime < _departureTime)
            {
                // We allow arrivalTime to equal Departure time, sometimes buses have less then a minute to travel
                // If there is still to much time difference, the train was probably cancelled, so we throw it out.
                throw new ArgumentException(
                    $"WTF? Time travellers! {_departureTime} incl {depDel} --> {_arrivalTime} incl {arrDel}\n{json}");
            }

            if (_departureStop == null || _arrivalStop == null)
            {
                throw new ArgumentNullException("_departureStop", "_arrivalStop");
            }
        }


        public Uri Operator()
        {
            return new Uri(GtfsTrip.AbsoluteUri);
        }

        public string Mode()
        {
            return GtfsTrip.AbsoluteUri;
        }

        public Uri Trip()
        {
            return GtfsTrip;
        }

        public Uri Route()
        {
            return GtfsRoute;
        }

        public Uri DepartureLocation()
        {
            return _departureStop;
        }

        public Uri ArrivalLocation()
        {
            return _arrivalStop;
        }

        public DateTime ArrivalTime()
        {
            return _arrivalTime;
        }

        public DateTime DepartureTime()
        {
            return _departureTime;
        }

        public Route AsRoute(ILocationProvider locationProv)
        {
            var depLoc = locationProv.GetCoordinateFor(_departureStop);
            var arrLoc = locationProv.GetCoordinateFor(_arrivalStop);

            return new Route
            {
                Shape = new[]
                {
                    new Coordinate(depLoc.Lat, depLoc.Lon),
                    new Coordinate(arrLoc.Lat, arrLoc.Lon)
                },
                ShapeMeta = new[]
                {
                    new Route.Meta
                    {
                        Profile = Mode(),
                        Shape = 0,
                        Time = 0f,
                    },
                    new Route.Meta
                    {
                        Profile = Mode(),
                        Shape = 1,
                        Time = (float) (ArrivalTime() - DepartureTime()).TotalSeconds,
                    },
                }
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is LinkedConnection lc)
            {
                return Equals(lc);
            }

            return false;
        }

        private bool Equals(LinkedConnection other)
        {
            return Equals(_departureStop, other._departureStop)
                   && Equals(_arrivalStop, other._arrivalStop)
                   && DepartureTime().Equals(other.DepartureTime())
                   && ArrivalTime().Equals(other.ArrivalTime())
                ;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_departureStop != null ? _departureStop.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_arrivalStop != null ? _arrivalStop.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DepartureTime().GetHashCode();
                hashCode = (hashCode * 397) ^ ArrivalTime().GetHashCode();
                return hashCode;
            }
        }
    }
}