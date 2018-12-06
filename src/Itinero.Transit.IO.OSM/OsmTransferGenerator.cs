using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using static Itinero.Osm.Vehicles.Vehicle;
using Profile = Itinero.Profiles.Profile;

namespace Itinero.Transit
{
    using UnixTime = UInt32;
    using LocId = UInt64;


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
        private readonly float _speed;
        private const float SearchDistance = 50f;
        private readonly StopsDb.StopsDbReader _stopsDb;

        // When  router db is loaded, it is saved into this dict to avoid reloading it
        private static readonly Dictionary<string, Router> KnownRouters
            = new Dictionary<string, Router>();


        /// <summary>
        /// Generate a new transfer generator, which takes into account
        /// the time needed to transfer, walk, ...
        ///
        /// Footpaths are generated using an Osm-based router database
        /// </summary>
        /// <param name="routerdbPath">To create paths</param>
        /// <param name="speed">The walking speed (in meter/second)</param>
        /// <param name="internalTransferTime">How many seconds does it take to go from one platform to another. Default is 180s</param>
        /// <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public OsmTransferGenerator(string routerdbPath, StopsDb.StopsDbReader stopsReader,
            float speed = 1.3f,
            int internalTransferTime = 180,
            Profile walkingProfile = null)
        {
            _speed = speed;
            _stopsDb = stopsReader;

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
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private Route CreateRouteBetween(ulong start, ulong end)
        {
            _stopsDb.MoveTo(start);
            var lat = (float) _stopsDb.Latitude;
            var lon = (float) _stopsDb.Longitude;

            _stopsDb.MoveTo(end);
            var latE = (float) _stopsDb.Latitude;
            var lonE = (float) _stopsDb.Longitude;

            var startPoint = _router.TryResolve(_walkingProfile, lat, lon, SearchDistance);
            var endPoint = _router.TryResolve(_walkingProfile, latE, lonE, SearchDistance);

            if (startPoint.IsError || endPoint.IsError)
            {
                return null;
            }

            var route = _router.TryCalculate(_walkingProfile, startPoint.Value, endPoint.Value);
            if (route.IsError)
            {
                return null;
            }

            return route.Value;
        }

        public Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, IConnection c) where T : IJourneyStats<T>
        {
            if (c.DepartureTime < buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave departs before the journey arrives. Are you building backward routes? Use the other method (CreateArrivingTransfer)");
            }

            if (c.DepartureLocation == buildOn.Location)
            {
                return null;
            }

            var route = CreateRouteBetween(buildOn.Location, c.DepartureLocation);

            var timeAvailable = c.DepartureTime - buildOn.Time;
            if (timeAvailable < route.TotalTime)
            {
                // Not enough time to walk
                return null;
            }


            var withWalk = buildOn.ChainSpecial(
                Journey<T>.WALK, (uint) (route.TotalTime + c.DepartureTime), c.DepartureLocation);
            return withWalk.ChainForward(c);
        }

        public Journey<T> CreateArrivingTransfer<T>(Journey<T> buildOn, IConnection c) where T : IJourneyStats<T>
        {
            if (c.ArrivalTime > buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave arrives after the rest journey departs. Are you building forward routes? Use the other method (CreateDepartingTransfer)");
            }

            if (c.ArrivalLocation == buildOn.Location)
            {
                return null;
            }

            var route = CreateRouteBetween(buildOn.Location, c.DepartureLocation);

            var timeAvailable = c.DepartureTime - buildOn.Time;
            if (timeAvailable < route.TotalTime)
            {
                // Not enough time to walk
                return null;
            }


            var withWalk = buildOn.ChainSpecial(
                Journey<T>.WALK, (uint) (c.ArrivalTime - route.TotalTime), c.ArrivalLocation);
            return withWalk.ChainBackward(c);
        }
    }
}