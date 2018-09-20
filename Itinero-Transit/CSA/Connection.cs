using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public class Connection : LinkedObject
    {
        public Uri DepartureStop { get; set; }
        public Uri ArrivalStop { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        /// <summary>
        /// Departure delay in seconds
        /// </summary>
        public int DepartureDelay { get; set; }

        /// <summary>
        /// Arrival delay in seconds
        /// </summary>
        public int ArrivalDelay { get; set; }

        /// <summary>
        /// Human readable name where the vehicle is heading (e.g. "Brugge")
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


        public Connection(Uri uri) : base(uri)
        {
        }

        public Connection(JToken json) : base(new Uri(json["@id"].ToString()))
        {
            DepartureStop = new Uri(json["departureStop"].ToString());
            ArrivalStop = new Uri(json["arrivalStop"].ToString());
            DepartureTime = DateTime.Parse(json["departureTime"].ToString());
            ArrivalTime = DateTime.Parse(json["arrivalTime"].ToString());
            DepartureDelay = GetInt(json, "departureDelay");
            ArrivalDelay = GetInt(json, "arrivalDelay");
            Direction = json["direction"].ToString();
            GtfsTrip = new Uri(json["gtfs:trip"].ToString());
            GtfsRoute = new Uri(json["gtfs:route"].ToString());
        }

        private static int GetInt(JToken json, string name)
        {
            var jtoken = json[name];
            return jtoken == null ? 0 : 
                int.Parse(jtoken.ToString());
        }


        public override string ToString()
        {
            return $"Connection {DepartureStop.Segments.Last()}:{DepartureTime:yyyy-MM-dd HH:mm:ss} --> {ArrivalStop.Segments.Last()}:{ArrivalTime:yyyy-MM-dd HH:mm:ss} ({Uri})";
        }
    }
}