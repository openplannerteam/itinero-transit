using System;
using System.Collections.Generic;
using Serilog;

namespace Itinero.Transit
{
    /// <summary>
    /// Tracks which stops have already been visited
    /// </summary>
    public class ActiveLocationTracker<T>
        where T : IJourneyStats<T>
    {
        /// <summary>
        ///  Locations that have already been seen
        /// </summary>
        private readonly HashSet<string> _locations = new HashSet<string>();

        /// <summary>
        /// Walks from the key location to close by stations
        /// </summary>
        private readonly Dictionary<string, List<IContinuousConnection>>
            _closeByLocations = new Dictionary<string, List<IContinuousConnection>>();


        private readonly DateTime _defaultTime;
        private readonly int _searchRadius;
        private readonly IFootpathTransferGenerator _router;
        private readonly ILocationProvider _backProvider;

        public ActiveLocationTracker(DateTime defaultTime, Profile<T> profile)
        {
            _defaultTime = defaultTime;
            _searchRadius = profile.IntermodalStopSearchRadius;
            _router = profile.FootpathTransferGenerator;
            _backProvider = profile.LocationProvider;
        }

        public void AddKnownLocation(Uri uri)
        {
            AddKnownLocation(_backProvider.GetCoordinateFor(uri));
        }

        public void AddKnownLocation(Location newLoc)
        {

            if (_searchRadius < 1)
            {
                // Intermodal search is disabled anyway    
                return;
            }
            
            var newName = newLoc.Uri.ToString();
            
            if (_locations.Contains(newName))
            {
                // Location already seen
                return;
            }

            _locations.Add(newName);
            // we calculate our close by locations to create walks from them
            var closeByLocations = _backProvider.GetLocationsCloseTo(newLoc.Lat, newLoc.Lon, _searchRadius);

            foreach (var closeLoc in closeByLocations)
            {
                var dep = _backProvider.GetCoordinateFor(closeLoc);
                if (dep == newLoc)
                {
                    continue;
                }
                
                
                var walk = _router.GenerateFootPaths(_defaultTime, dep,
                    newLoc);
                if (walk == null)
                {
                    Log.Warning(
                        $"Could not find a way between {_backProvider.GetNameOf(dep.Id())} and {_backProvider.GetNameOf(newLoc.Id())}");
                    continue;
                }


                var depKey = walk.DepartureLocation().ToString();
                if (!_closeByLocations.ContainsKey(depKey))
                {
                    _closeByLocations[depKey] = new List<IContinuousConnection>();
                }

                _closeByLocations[depKey].Add(walk);
            }
        }

        public IEnumerable<IContinuousConnection> WalksFrom(Uri uri)
        {
            return _closeByLocations.GetValueOrDefault(uri.ToString(), null);
        }
    }
}