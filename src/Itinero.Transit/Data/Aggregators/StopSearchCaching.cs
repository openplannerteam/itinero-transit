using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Data.Aggregators
{
    public static class StopReaderExtensions
    {
        public static IStopsReader UseCache(this IStopsReader stopsReader)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (stopsReader is StopSearchCaching)
            {
                return stopsReader;
            }

            return new StopSearchCaching(stopsReader);
        }
    }


    public class StopSearchCaching : IStopsReader
    {
        private readonly IStopsReader _stopsReader;

        private uint _miss, _hit;

        private Dictionary<(double, double, double, double), IEnumerable<IStop>>
            cache = new Dictionary<(double, double, double, double), IEnumerable<IStop>>();

        private Dictionary<(double lon, double lat, double maxDistance), IStop>
            cacheClosest = new Dictionary<(double, double, double), IStop>();

        private Dictionary<(double lat, double lon, double range), IEnumerable<IStop>>
            cacheInRange = new Dictionary<(double lat, double lon, double range), IEnumerable<IStop>>();

        public StopSearchCaching(IStopsReader stopsReader)
        {
            _stopsReader = stopsReader;
        }


        public IEnumerable<IStop> LocationsInRange(double lat, double lon, double range)
        {
            var key = (lat, lon, range);
            if (cacheInRange.ContainsKey(key))
            {
                _hit++;
                return cacheInRange[key];
            }

            _miss++;
            return cacheInRange[key] = StopSearch.LocationsInRange(this, lat, lon, range);
        }


        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            if (cache.ContainsKey(box))
            {
                _hit++;
                return cache[box];
            }

            _miss++;
            var v = _stopsReader.SearchInBox(box).ToList();
            cache[box] = v;
            return v;
        }

        internal void ResetCounters()
        {
            _miss = 0;
            _hit = 0;
        }

        internal void DumpCacheTotals()
        {
            Log.Information($"Caching information: {_miss} miss, {_hit} hit, {_miss + _hit} total");
        }

        public IStop SearchClosest(double lon, double lat, double maxDistanceInMeters = 1000)
        {
            var k = (lon, lat, maxDistanceInMeters);
            if (cacheClosest.ContainsKey(k))
            {
                return cacheClosest[k];
            }

            // Directly use 'this' to perhaps hit caching
            var v = StopSearch.SearchClosest(this, lon, lat, maxDistanceInMeters);
            cacheClosest[k] = v;
            return v;
        }


        // ----------- Only boring, generated code below ------------ //        


        public string GlobalId => _stopsReader.GlobalId;

        public LocationId Id => _stopsReader.Id;

        public double Longitude => _stopsReader.Longitude;

        public double Latitude => _stopsReader.Latitude;

        public IAttributeCollection Attributes => _stopsReader.Attributes;

        public bool MoveTo(LocationId stop)
        {
            return _stopsReader.MoveTo(stop);
        }

        public bool MoveTo(string globalId)
        {
            return _stopsReader.MoveTo(globalId);
        }

        public bool MoveNext()
        {
            return _stopsReader.MoveNext();
        }

        public void Reset()
        {
            _stopsReader.Reset();
        }

        public List<IStopsReader> UnderlyingDatabases => _stopsReader.UnderlyingDatabases;

        public StopsDb StopsDb => _stopsReader.StopsDb;
    }
}