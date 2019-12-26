using System.Collections.Generic;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.LocationIndexing
{
    /// <summary>
    /// The RTree location index builds a tree to split the points.
    /// All points are added onto a squared tile;
    /// if there are more then N points into this tile, the tile is split in 4 subtiles.
    ///
    /// Note: the tiles follow the slippy map zoomlevel system
    /// </summary>
    public class RTreeLocationindex<T> : ILocationIndexing<T>
    {
        private readonly uint _maxEntries;

        public RTreeLocationindex(uint maxEntries = 2048)
        {
            _maxEntries = maxEntries;
        }

        public IEnumerable<T> Get((int x, int y) tile)
        {
            throw new System.NotImplementedException();
        }

        public List<T> GetInRange((double lat, double lon) c, uint maxDistanceInMeter)
        {
            throw new System.NotImplementedException();
        }

        private class Tile
        {
            private readonly uint _maxEntries;
            public uint Zoomlevel;
            private readonly int _x;
            private readonly int _y;
            public double MinLat, MaxLat, MinLon, MaxLon;
            
            private List<((double lon, double lat), T)> _data = new List<((double lon, double lat), T)>();
            private Tile _upperleft, _upperright, _lowerleft, _lowerright;

            public Tile(uint maxEntries, uint zoomlevel, int x, int y)
            {
                _maxEntries = maxEntries;
                Zoomlevel = zoomlevel;
                _x = x;
                _y = y;
                (MaxLat, MinLon) = DistanceEstimate.NorthWestCoordinateOfTile((x, y), zoomlevel);
                (MaxLat, MinLon) = DistanceEstimate.NorthWestCoordinateOfTile((x+1, y+1), zoomlevel);
            }

            public void Add((double lon, double lat) c, T t)
            {
                if (_data.Count < _maxEntries)
                {
                    _data.Add((c, t));
                }
            }
        }
    }
}