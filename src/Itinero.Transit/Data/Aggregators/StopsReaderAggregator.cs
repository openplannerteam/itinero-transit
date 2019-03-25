using System.Collections.Generic;
using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsReaderAggregator : IStopsReader
    {
        private IStopsReader _currentStop;

        public List<IStopsReader> UnderlyingDatabases { get; }

        public StopsReaderAggregator(List<IStopsReader> stops)
        {
            UnderlyingDatabases = stops;
        }

        public bool MoveTo(LocationId stop)
        {
            _currentStop = UnderlyingDatabases[(int) stop.DatabaseId];
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
            foreach (var reader in UnderlyingDatabases)
            {
                reader.Reset();
            }
        }


        public string GlobalId => _currentStop.GlobalId;

        public LocationId Id => _currentStop.Id;

        public double Longitude => _currentStop.Longitude;

        public double Latitude => _currentStop.Latitude;

        public IAttributeCollection Attributes => _currentStop.Attributes;

        public StopsDb StopsDb => _currentStop.StopsDb;
    }
}