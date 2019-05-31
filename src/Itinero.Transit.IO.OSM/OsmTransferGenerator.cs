using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using static Itinero.Osm.Vehicles.Vehicle;
using Profile = Itinero.Profiles.Profile;

namespace Itinero.Transit
{
    using UnixTime = UInt32;


    /// <inheritdoc />
    /// <summary>
    /// The transfer generator has the responsibility of creating
    /// transfers between multiple locations, possibly intermodal.
    /// If the departure and arrival location are the same, an internal
    /// transfer is generate.
    /// If not, the OpenStreetMap database is queried to generate a path between them.
    /// </summary>
    public class OsmTransferGenerator : IOtherModeGenerator
    {
        private readonly Router _router;
        private readonly Profile _walkingProfile;

        private const float SearchDistance = 50f;
     

        // When  router db is loaded, it is saved into this dict to avoid reloading it
        private static readonly Dictionary<string, Router> KnownRouters
            = new Dictionary<string, Router>();


        ///  <summary>
        ///  Generate a new transfer generator, which takes into account
        ///  the time needed to transfer, walk, ...
        /// 
        ///  Footpaths are generated using an Osm-based router database
        ///  </summary>
        ///  <param name="routerdbPath">To create paths</param>
        ///  <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public OsmTransferGenerator(string routerdbPath,
            Profile walkingProfile = null)
        {
            
            _walkingProfile = walkingProfile ?? Pedestrian.Fastest();
            routerdbPath = Path.GetFullPath(routerdbPath);
            if (!KnownRouters.ContainsKey(routerdbPath))
            {
                using (var fs = new FileStream(routerdbPath, FileMode.Open, FileAccess.Read))
                {
                    var routerDb = RouterDb.Deserialize(fs);
                    if (routerDb == null)
                    {
                        throw new NullReferenceException("Could not load the routerDb");
                    }

                    KnownRouters[routerdbPath] = new Router(routerDb);
                }
            }

            _router = KnownRouters[routerdbPath];
        }


        /// <summary>
        /// Tries to calculate a route between the two given point.
        /// Can be null if no route could be determined
        /// </summary>
        /// <returns></returns>
        private Route CreateRouteBetween(IStopsReader stopsDb, LocationId start,
            LocationId end)
        {
            stopsDb.MoveTo(start);
            var lat = (float) stopsDb.Latitude;
            var lon = (float) stopsDb.Longitude;

            stopsDb.MoveTo(end);
            var latE = (float) stopsDb.Latitude;
            var lonE = (float) stopsDb.Longitude;

            // ReSharper disable once RedundantArgumentDefaultValue
            var startPoint = _router.TryResolve(_walkingProfile, lat, lon, SearchDistance);
            // ReSharper disable once RedundantArgumentDefaultValue
            var endPoint = _router.TryResolve(_walkingProfile, latE, lonE, SearchDistance);

            if (startPoint.IsError || endPoint.IsError)
            {
                return null;
            }

            var route = _router.TryCalculate(_walkingProfile, startPoint.Value, endPoint.Value);
            return route.IsError ? null : route.Value;
        }

        public Journey<T> CreateDepartureTransfer<T>(IStopsReader stops, Journey<T> buildOn, ulong timeWhenDeparting,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {
            if (timeWhenDeparting < buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave departs before the journey arrives. Are you building backward routes? Use the other method (CreateArrivingTransfer)");
            }

            if (Equals(otherLocation, buildOn.Location))
            {
                return null;
            }

            var route = CreateRouteBetween(stops, buildOn.Location, otherLocation);

            var timeAvailable = timeWhenDeparting - buildOn.Time;
            if (timeAvailable < route.TotalTime)
            {
                // Not enough time to walk
                return null;
            }

            return buildOn.ChainSpecial(
                Journey<T>.WALK, (uint) (buildOn.Time + route.TotalDistance), otherLocation,
                TripId.Walk);
        }

        public Journey<T> CreateArrivingTransfer<T>(IStopsReader stops, Journey<T> buildOn, ulong timeWhenArriving,
            LocationId otherLocation) where T : IJourneyMetric<T>
        {
            if (timeWhenArriving > buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave arrives after the rest journey departs. Are you building forward routes? Use the other method (CreateDepartingTransfer)");
            }

            if (Equals(otherLocation, buildOn.Location))
            {
                return null;
            }

            var route = CreateRouteBetween(stops, buildOn.Location, otherLocation);

            var timeAvailable = buildOn.Time - timeWhenArriving;
            if (timeAvailable < route.TotalTime)
            {
                // Not enough time to walk
                return null;
            }


            return buildOn.ChainSpecial(
                Journey<T>.WALK, (uint) (timeWhenArriving + route.TotalTime), otherLocation, TripId.Walk);
        }

        public float TimeBetween(IStopsReader reader, LocationId @from, LocationId to)
        {
            if (Equals(from, to))
            {
                return float.NaN;
            }

            var route = CreateRouteBetween(reader, from, to);
            return route.TotalTime;
        }

        public float Range()
        {
            return SearchDistance;
        }
    }
}