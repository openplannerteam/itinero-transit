using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;

namespace Itinero.Transit.Data.Simple
{
    public class SimpleStopsDb : SimpleDb<StopId, Stop>, IClone<SimpleStopsDb>, IStopsDb
    {
        private TiledLocationIndexing<Stop> _locationIndex;
        public ILocationIndexing<Stop> LocationIndex => _locationIndex;

        public SimpleStopsDb(uint dbId) : base(dbId)
        {
        }

        public SimpleStopsDb(SimpleStopsDb copyFrom) : base(copyFrom)
        {
            PostProcess(copyFrom._locationIndex?.ZoomLevel ?? 14);
        }

        public void PostProcess(uint zoomLevel)
        {
            _locationIndex = new TiledLocationIndexing<Stop>(zoomLevel);
            foreach (var stop in Data)
            {
                _locationIndex.Add(stop.Longitude, stop.Latitude, stop);
            }
        }

        public SimpleStopsDb Clone()
        {
            return new SimpleStopsDb(this);
        }

        IStopsDb IClone<IStopsDb>.Clone()
        {
            return Clone();
        }
    }
}