using System.Collections.Generic;
using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsReaderAggregator : IStopsReader
    {
        private IStopsReader _currentStop;

        private readonly List<IStopsReader> _stops;

        public StopsReaderAggregator(List<IStopsReader> stops)
        {
            _stops = stops;
        }

        public bool MoveTo((uint localTileId, uint localId) stop)
        {
            throw new System.NotImplementedException();
        }

        public bool MoveTo(string globalId)
        {
            foreach (var stop in _stops)
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
            foreach (var reader in _stops)
            {
                reader.Reset();
            }
        }


        public string GlobalId => _currentStop.GlobalId;

        public (uint tileId, uint localId) Id => _currentStop.Id;

        public double Longitude => _currentStop.Longitude;

        public double Latitude => _currentStop.Latitude;

        public IAttributeCollection Attributes => _currentStop.Attributes;

        public StopsDb StopsDb => _currentStop.StopsDb;
    }
}