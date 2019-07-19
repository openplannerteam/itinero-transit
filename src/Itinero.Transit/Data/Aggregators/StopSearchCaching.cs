using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopSearchCaching : IStopsReader
    {
        private readonly IStopsReader _stopsReader;

        private readonly Dictionary<(double, double, double, double), IEnumerable<IStop>> _cache;

        public StopSearchCaching(IStopsReader stopsReader)
        {
            _cache = new Dictionary<(double, double, double, double), IEnumerable<IStop>>();
            _stopsReader = stopsReader;
        }

        // ReSharper disable once UnusedMember.Global
        public StopSearchCaching(IStopsReader stopsReader, StopSearchCaching shareCacheWith)
        {
            _stopsReader = stopsReader;
            _cache = shareCacheWith._cache;
        }


        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            if (_cache.ContainsKey(box))
            {
                return _cache[box];
            }

            var v = _stopsReader.SearchInBox(box).ToList();
            _cache[box] = v;
            return v;
        }


        // ----------- Only boring, generated code below ------------ //        


        public string GlobalId => _stopsReader.GlobalId;

        public StopId Id => _stopsReader.Id;

        public double Longitude => _stopsReader.Longitude;

        public double Latitude => _stopsReader.Latitude;

        public IAttributeCollection Attributes => _stopsReader.Attributes;

        public bool MoveTo(StopId stop)
        {
            return _stopsReader.MoveTo(stop);
        }

        public bool MoveTo(string globalId)
        {
            return _stopsReader.MoveTo(globalId);
        }

        public HashSet<uint> DatabaseIndexes()
        {
            return _stopsReader.DatabaseIndexes();
        }

        public bool MoveNext()
        {
            return _stopsReader.MoveNext();
        }

        public void Reset()
        {
            _stopsReader.Reset();
        }
    }
}