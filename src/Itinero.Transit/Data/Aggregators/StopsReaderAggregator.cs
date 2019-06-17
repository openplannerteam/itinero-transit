using System;
using System.Collections.Generic;
using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsReaderAggregator : IStopsReader
    {
        private IStopsReader _currentStop;


        public List<IStopsReader> UnderlyingDatabases { get; }
        private int _currentIndex;


        public static IStopsReader CreateFrom(IEnumerable<TransitDb.TransitDbSnapShot> snapShot)
        {
            var enumerators = new List<IStopsReader>();

            foreach (var dbSnapShot in snapShot)
            {
                enumerators.Add(dbSnapShot.StopsDb.GetReader());
            }

            return CreateFrom(enumerators);
        }

        public static IStopsReader CreateFrom(List<IStopsReader> enumerators)
        {
            if (enumerators.Count == 0)
            {
                throw new Exception("No enumerators found");
            }

            if (enumerators.Count == 0)
            {
                return enumerators[0];
            }

            return new StopsReaderAggregator(enumerators);
        }

        private StopsReaderAggregator(List<IStopsReader> stops)
        {
            UnderlyingDatabases = stops;
            _currentStop = stops[_currentIndex];
        }

        public bool MoveNext()
        {
            while (_currentIndex < UnderlyingDatabases.Count)
            {
                if (_currentStop.MoveNext())
                {
                    return true;
                }

                _currentIndex++;
                if (_currentIndex == UnderlyingDatabases.Count)
                {
                    return false;
                }

                _currentStop = UnderlyingDatabases[_currentIndex];
            }

            return false;
        }

        public bool MoveTo(LocationId stop)
        {
            foreach (var stopsReader in UnderlyingDatabases)
            {
                // ReSharper disable once InvertIf
                if (stopsReader.MoveTo(stop))
                {
                    _currentStop = stopsReader;
                    return true;
                }
            }

            return false;
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