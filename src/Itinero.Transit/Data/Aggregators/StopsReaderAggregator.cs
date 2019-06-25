using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsReaderAggregator : IStopsReader
    {
        private IStopsReader _currentStop;

        public static IStopsReader CreateFrom(IEnumerable<TransitDb.TransitDbSnapShot> snapShot)
        {
            var enumerators = new List<IStopsReader>();

            foreach (var dbSnapShot in snapShot)
            {
                enumerators.Add(dbSnapShot.StopsDb.GetReader());
            }

            return CreateFrom(enumerators);
        }

        private IStopsReader[] UnderlyingDatabases;
        private int _currentIndex;
        private readonly HashSet<uint> _responsibleFor;


        public static IStopsReader CreateFrom(List<IStopsReader> stopsReaders)
        {
            if (stopsReaders.Count == 0)
            {
                throw new Exception("No enumerators found");
            }

            if (stopsReaders.Count == 1)
            {
                return stopsReaders[0];
            }

            return new StopsReaderAggregator(stopsReaders);
        }

        private StopsReaderAggregator(List<IStopsReader> stops)
        {
            var expanded = new List<IStopsReader>();
            _responsibleFor = new HashSet<uint>();

            foreach (var stop in stops)
            {
                if (stop is StopsReaderAggregator aggr)
                {
                    expanded.AddRange(aggr.UnderlyingDatabases);
                }
                else
                {
                    expanded.Add(stop);
                }

                _responsibleFor.UnionWith(stop.DatabaseIndexes());
            }

            var max = _responsibleFor.Max();
            UnderlyingDatabases = new IStopsReader[max + 1];

            foreach (var stopsReader in expanded)
            {
                foreach (var index in stopsReader.DatabaseIndexes())
                {
                    UnderlyingDatabases[index] = stopsReader;
                }
            }

            _currentStop = stops[_currentIndex];
        }


        public HashSet<uint> DatabaseIndexes()
        {
            return _responsibleFor;
        }

        public bool MoveNext()
        {
            while (_currentIndex < UnderlyingDatabases.Length)
            {
                if (_currentStop.MoveNext())
                {
                    return true;
                }

                _currentIndex++;
                if (_currentIndex == UnderlyingDatabases.Length)
                {
                    return false;
                }

                _currentStop = UnderlyingDatabases[_currentIndex];
            }

            return false;
        }

        public bool MoveTo(LocationId stop)
        {
            _currentStop = UnderlyingDatabases[stop.DatabaseId];
            return _currentStop.MoveTo(stop);
        }

        public bool MoveTo(string globalId)
        {
            foreach (var stop in UnderlyingDatabases)
            {
                // ReSharper disable once InvertIf
                if (stop.MoveTo(globalId))
                {
                    _currentStop = stop;
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _currentIndex = 0;
            foreach (var reader in UnderlyingDatabases)
            {
                reader.Reset();
            }
        }

        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var stops = new List<IStop>();
            foreach (var db in UnderlyingDatabases)
            {
                stops.AddRange(db.SearchInBox(box));
            }

            return stops;
        }

        public string GlobalId => _currentStop.GlobalId;

        public LocationId Id => _currentStop.Id;

        public double Longitude => _currentStop.Longitude;

        public double Latitude => _currentStop.Latitude;

        public IAttributeCollection Attributes => _currentStop.Attributes;
    }
}