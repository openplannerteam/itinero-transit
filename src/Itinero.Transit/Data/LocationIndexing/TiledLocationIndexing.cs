using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.LocationIndexing
{
    /// <summary>
    /// This class divides all the stops into cells, in order to quickly determine
    /// - which stops are in a certain bounding box
    /// - which stops are within a certain radius of a given location.
    ///
    /// All locations here have a "Longitude, Latitude" in WGS84
    /// </summary>
    public class TiledLocationIndexing<T> : ILocationIndexing<T>
    {
        public readonly uint ZoomLevel;

        private readonly Dictionary<(int x, int y), List<T>>
            _dataPerTile = new Dictionary<(int x, int y), List<T>>();

        public TiledLocationIndexing(uint zoomLevel = 14)
        {
            ZoomLevel = zoomLevel;
        }

        public void Add(double lon, double lat, T t)
        {
            var key = DistanceEstimate.Wgs84ToTileNumbers((lon, lat), ZoomLevel);
            if (!_dataPerTile.TryGetValue(key, out var data))
            {
                data = new List<T>();
                _dataPerTile[key] = data;
            }

            data.Add(t);
        }

        public IEnumerable<T> GetInBox((double minlon, double maxlat) nw, (double maxlon, double minlat) se)
        {
            var (firstX, firstY) = DistanceEstimate.Wgs84ToTileNumbers(nw, ZoomLevel);
            var (lastX, lastY) = DistanceEstimate.Wgs84ToTileNumbers(se, ZoomLevel);
            return _dataPerTile
                .Where(kv =>
                {
                    var x = kv.Key.x;
                    var y = kv.Key.y;
                    return (firstX <= x && x <= lastX && firstY <= y && y <= lastY);
                }).SelectMany(kv => kv.Value);
        }


        /// <summary>
        /// Gets all data which are in range of the given coordinate.
        /// Note that the actual data may be positioned slightly further then the given maxDistance.
        /// </summary>
        /// <returns></returns>
        [Pure]
        public List<T> GetInRange((double lon, double lat) c, double maxDistanceInMeter)
        {
            var result = new List<T>();

            // First, lets figure out the bounding box

            var centerTile = DistanceEstimate.Wgs84ToTileNumbers(c, ZoomLevel);
            var centerTileNw = DistanceEstimate.NorthWestCoordinateOfTile(centerTile, ZoomLevel);
            var (width, height) = DistanceEstimate.SizeOf(centerTile, ZoomLevel);

            var diffX = (uint) Math.Ceiling(maxDistanceInMeter / width / 2);
            var diffY = (uint) Math.Ceiling(maxDistanceInMeter / height / 2);

            var firstX = centerTile.x - diffX;
            var lastX = centerTile.x + diffY;

            var firstY = centerTile.y - diffY;
            var lastY = centerTile.y + diffY;


            foreach (var kv in _dataPerTile)
            {
                var (x, y) = kv.Key;
                var data = kv.Value;

                if (!(firstX <= x && x <= lastX && firstY <= y && y <= lastY))
                {
                    // Out of the bounding box
                    continue;
                }

                var closestX = x;
                if (x < centerTile.x)
                {
                    // We are on the left of the centertile, the closest side is one tile to the right
                    // x1 < x2 ==> lon1 < lon2
                    
                    closestX++;
                }

                var closestY = y;
                if (y < centerTile.y)
                {
                    // We are above the center tile, the closest side is one tile beneath
                    // y1 < y2 ==> lat1 > lat2
                    closestY++;
                }

                var closestCornerCoordinate =
                    DistanceEstimate.NorthWestCoordinateOfTile((closestX, closestY), ZoomLevel);
                var closestDistance = DistanceEstimate.DistanceEstimateInMeter(closestCornerCoordinate, centerTileNw);
                if (closestDistance > maxDistanceInMeter)
                {
                    continue;
                }

                result.AddRange(data);
            }

            return result;
        }
    }
}