using System;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA
{
    /**
     * A connection represents a single connection someone can take.
     * It consists of a departure and arrival stop, departure and arrival time.
     * Note that a connection does _never_ have intermediate stops.
     *
     * The saved data is more then useful for barebones route planner, it is simply everything that IRail offered
     * 
     */
    [Serializable()]
    public class LinkedConnection : LinkedObject, IConnection
    {
        public Uri DepartureStop { get; set; }
        public Uri ArrivalStop { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

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
        /// Looks very much the same as gtfs_trip (on irail they are identical)
        /// </summary>
        public Uri GtfsRoute { get; set; }


        public LinkedConnection(Uri uri) : base(uri)
        {
        }

        public LinkedConnection(JObject json) : base(new Uri(json["@id"].ToString()))
        {
            FromJson(json);
        }



        public override string ToString()
        {
            return
                $"Train connection by NMBS, {Stations.GetName(DepartureStop)} {DepartureTime:yyyy-MM-dd HH:mm:ss} --> {Stations.GetName(ArrivalStop)}" +
                $" {ArrivalTime:yyyy-MM-dd HH:mm:ss}\n    Direction {Direction} ({Uri})";
        }

        protected sealed override void FromJson(JObject json)
        {
            
            AssertTypeIs(json, "http://semweb.mmlab.be/ns/linkedconnections#Connection");

            DepartureStop = GetId(json, "http://semweb.mmlab.be/ns/linkedconnections#departureStop");
            ArrivalStop = GetId(json, "http://semweb.mmlab.be/ns/linkedconnections#arrivalStop");

            var depDel = GetInt(json, "http://semweb.mmlab.be/ns/linkedconnections#departureDelay");
            // Departure time already includes delay
            DepartureTime =
                DateTime.Parse(GetValue(json, "http://semweb.mmlab.be/ns/linkedconnections#departureTime"));
            
            
            var arrDel = GetInt(json, "http://semweb.mmlab.be/ns/linkedconnections#arrivalDelay");
            // Arrival time already includes delay
            ArrivalTime = DateTime.Parse(
                GetValue(json, "http://semweb.mmlab.be/ns/linkedconnections#arrivalTime"));
                
            Direction = GetValue(json, "http://vocab.gtfs.org/terms#headsign");
            GtfsTrip = GetId(json, "http://vocab.gtfs.org/terms#trip");
            GtfsRoute = GetId(json, "http://vocab.gtfs.org/terms#route");
           
            
            if (ArrivalTime < DepartureTime)
            {
                // TODO This is a workaround for issue https://github.com/iRail/iRail/issues/361
                // Sometimes, a departure delay is already known but the arrivaldelay is not known yet
                // Thus, the arrivalDelay defaults to 0
                // This can lead to (esp. on short connections of only a few minutes) departuretimes which lie _after_
                // the arrivaltime
                // We fix this by estimating the arrivaldelay to be equal to the departureDelay
                depDel += arrDel;
                ArrivalTime = ArrivalTime.AddSeconds(depDel);
            }

            if (ArrivalTime < DepartureTime)
            {
                // If there is still to much time difference, the train was probably cancelled, so we throw it out.
                throw new ArgumentException(
                    $"WTF? Timetravellers! {DepartureTime} incl {depDel} --> {ArrivalTime} incl {arrDel}\n{json}");
            }
        }


        public Uri Operator()
        {
            return new Uri("https://www.belgiantrain.be/");
        }

        public string Mode()
        {
            return "Train";
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
            return DepartureStop;
        }

        public Uri ArrivalLocation()
        {
            return ArrivalStop;
        }

        DateTime IConnection.ArrivalTime()
        {
            return ArrivalTime;
        }

        DateTime IConnection.DepartureTime()
        {
            return DepartureTime;
        }

        public bool Continuous()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SncbConnection conn))
            {
                return false;
            }
            return Equals(conn);
        }

        protected bool Equals(SncbConnection other)
        {
            return Equals(DepartureStop, other.DepartureStop) && Equals(ArrivalStop, other.ArrivalStop) && 
                   DepartureTime.Equals(other.DepartureTime) && ArrivalTime.Equals(other.ArrivalTime) &&
                   string.Equals(Direction, other.Direction) && Equals(GtfsTrip, other.GtfsTrip) && 
                   Equals(GtfsRoute, other.GtfsRoute);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (DepartureStop != null ? DepartureStop.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ArrivalStop != null ? ArrivalStop.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DepartureTime.GetHashCode();
                hashCode = (hashCode * 397) ^ ArrivalTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (Direction != null ? Direction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GtfsTrip != null ? GtfsTrip.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GtfsRoute != null ? GtfsRoute.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
    
}