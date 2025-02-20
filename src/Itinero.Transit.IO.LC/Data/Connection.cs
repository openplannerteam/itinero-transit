﻿using System;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Itinero.Transit.IO.LC.Data
{
    /**
     * A connection represents a single connection someone can take.
     * It consists of a departure and arrival stop, departure and arrival time.
     * Note that a connection does _never_ have intermediate stops.
     */
    [Serializable]
    public class Connection : ILinkedObject
    {
        private Uri _departureStop;
        private Uri _arrivalStop;
        
        public Uri Uri { get; }

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


        /// <summary>
        /// Indicates that this connection exceptionally doesn't go
        /// </summary>
        public bool IsCancelled { get; private set; }


        private const string GtfsRegular = "http://vocab.gtfs.org/terms#Regular";

        // ReSharper disable once UnusedMember.Global
        public Connection(Uri uri)
        {
            Uri = uri;
        }

        public Connection(JObject json)
        {
            Uri = json.GetId();
            FromJson(json);
        }


        public override string ToString()
        {
            return ToString(null);
        }


        private string ToString(LocationFragment locationDecoder)
        {
            return
                $"Linked Connection {locationDecoder?.GetNameOf(_departureStop) ?? _departureStop.ToString()} {_departureTime:HH:mm}" +
                $" --> {locationDecoder?.GetNameOf(_arrivalStop) ?? _arrivalStop.ToString()} {_arrivalTime:HH:mm}" +
                $"  {Uri}";
        }

        private static DateTime GetDateFixed(JToken json, string uriKey)
        {
            var value = json.GetLDValue(uriKey);
            return DateTime.Parse(value).ToUniversalTime();
        }

        public void FromJson(JObject json)
        {
            var isCancelledConnection = json.IsType("http://semweb.mmlab.be/ns/linkedconnections#CancelledConnection");
            if (!json.IsType("http://semweb.mmlab.be/ns/linkedconnections#Connection")
                && !isCancelledConnection)
            {
                throw new JsonException(
                    "Incorrect type: this connection is not a linked connection (neither is it a cancelled linked connection)");
            }

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

            if (_arrivalTime < _departureTime)
            {
                var message = $"Connection {Uri} goes back in time: {this}";
                Log.Error(message);
                throw new ArgumentException(message);
                
            }
            
            Direction = json.GetLDValue("http://vocab.gtfs.org/terms#headsign");
            GtfsTrip = json.GetId("http://vocab.gtfs.org/terms#trip");
            GtfsRoute = json.GetId("http://vocab.gtfs.org/terms#route");

            var getOn = json.GetContents("http://vocab.gtfs.org/terms#pickupType", GtfsRegular);

            GetOn = getOn.IsString() && getOn.ToString().Equals(GtfsRegular)
                    || getOn.GetId().ToString().Equals(GtfsRegular);

            IsCancelled = isCancelledConnection;

            var getOff = json.GetContents("http://vocab.gtfs.org/terms#dropOffType", GtfsRegular);
            GetOff = getOff.IsString() && getOff.ToString().Equals(GtfsRegular)
                     || getOff.GetId().ToString().Equals(GtfsRegular);

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


    }
}