using System;
using System.Diagnostics.CodeAnalysis;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.IO.LC.Data
{
    /**
     * A connection represents a single connection someone can take.
     * It consists of a departure and arrival stop, departure and arrival time.
     * Note that a connection does _never_ have intermediate stops.
     */
    [Serializable]
    public class Connection : LinkedObject
    {
        private Uri _departureStop;
        private Uri _arrivalStop;

        private DateTime _departureTime;
        private DateTime _arrivalTime;

        /// <summary>
        /// Gets or sets the arrival delay.
        /// </summary>
        public ushort ArrivalDelay { get; private set; }

        /// <summary>
        /// Gets or sets the departure delay.
        /// </summary>
        public ushort DepartureDelay { get; private set; }

        /// <summary>
        /// Human readable name where the vehicle is heading (e.g. "Brugge")
        /// Aka the 'headsign'
        /// </summary>
        public string Direction { get; private set; }

        /// <summary>
        /// URI of the current trip sequence, where _all_ the stops can be seen; with delays
        /// </summary>
        public Uri GtfsTrip { get; private set; }

        /// <summary>
        /// URI of the route of this train connection. (e.g. the train connection Oostende-Eupen) aspecific of time
        /// Looks very much the same as GTFS-trip (on IRail they are identical)
        /// </summary>
        public Uri GtfsRoute { get; private set; }

        /// <summary>
        /// Indicates that a traveller can get on to this connection
        /// </summary>
        public bool GetOn { get; private set; }
        
        /// <summary>
        /// Indicates that a traveller can get off this connection here
        /// </summary>
        public bool GetOff { get; private set; }



        // ReSharper disable once UnusedMember.Global
        public Connection(Uri uri) : base(uri)
        {
        }

        public Connection(Uri id, Uri departureStop, Uri arrivalStop, DateTime departureTime,
            DateTime arrivalTime) : base(id)
        {
            _departureTime = departureTime;
            _arrivalTime = arrivalTime;
            _arrivalStop = arrivalStop;
            _departureStop = departureStop;
        }

        public Connection(JObject json) : base(json.GetId())
        {
            FromJson(json);
        }


        public override string ToString()
        {
            return ToString(null);
        }


        public string ToString(LocationProvider locationDecoder)
        {
            return
                $"Linked Connection {locationDecoder?.GetNameOf(_departureStop) ?? _departureStop.ToString()} {_departureTime:HH:mm}" +
                $" --> {locationDecoder?.GetNameOf(_arrivalStop) ?? _arrivalStop.ToString()} {_arrivalTime:HH:mm}" +
                $"  {Uri}";
        }

        private static DateTime GetDateFixed(JToken json, string uriKey)
        {
            var value = json.GetLDValue(uriKey);
            return DateTime.Parse(value);
        }

        protected sealed override void FromJson(JObject json)
        {
            json.AssertTypeIs("http://semweb.mmlab.be/ns/linkedconnections#Connection");

            _departureStop = json.GetId("http://semweb.mmlab.be/ns/linkedconnections#departureStop");
            _arrivalStop = json.GetId("http://semweb.mmlab.be/ns/linkedconnections#arrivalStop");

            var depDel = json.GetInt("http://semweb.mmlab.be/ns/linkedconnections#departureDelay", 0);
            DepartureDelay = 0;
            if (depDel < ushort.MaxValue)
            {
                DepartureDelay = (ushort) depDel;
            }
            else
            {
                DepartureDelay = ushort.MaxValue;
            }

            // Departure time already includes delay
            _departureTime = GetDateFixed(json, "http://semweb.mmlab.be/ns/linkedconnections#departureTime");

            var arrDel = json.GetInt("http://semweb.mmlab.be/ns/linkedconnections#arrivalDelay", 0);
            ArrivalDelay = 0;
            if (arrDel < ushort.MaxValue)
            {
                ArrivalDelay = (ushort) arrDel;
            }
            else
            {
                ArrivalDelay = ushort.MaxValue;
            }

            // Arrival time already includes delay
            _arrivalTime = GetDateFixed(json, "http://semweb.mmlab.be/ns/linkedconnections#arrivalTime");

            Direction = json.GetLDValue("http://vocab.gtfs.org/terms#headsign");
            GtfsTrip = json.GetId("http://vocab.gtfs.org/terms#trip");
            GtfsRoute = json.GetId("http://vocab.gtfs.org/terms#route");
            

            GetOn = json.GetLDValue("http://vocab.gtfs.org/terms#pickupType")
                .Equals("gtfs:regular");
            GetOff = json.GetLDValue("http://vocab.gtfs.org/terms#dropOffType")
                .Equals("gtfs:regular");

            // ReSharper disable once InvertIf
            if (_arrivalTime <= _departureTime)
            {
                // Sometimes, a departure delay is already known but the arrival delay is not known yet
                // Thus, the arrivalDelay defaults to 0
                // This can lead to departure times which lie _after_ the arrival time,
                // especially on short connections of only a few minutes.
                // We attempt to fix this by estimating the arrival delay to be equal to the departureDelay
                // If that still ain't enough, the upstream code is responsible of handling the case (e.g. by logging, crashing or dropping this connection)
                depDel += arrDel;
                _arrivalTime = _arrivalTime.AddSeconds(depDel);
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is Connection lc)
            {
                return Equals(lc);
            }

            return false;
        }

        private bool Equals(Connection other)
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