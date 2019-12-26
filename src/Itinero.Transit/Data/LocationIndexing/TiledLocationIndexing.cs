using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

        private readonly IEnumerable<T> _empty = new T[0];

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
        public List<T> GetInRange((double lat, double lon) c, double maxDistanceInMeter)
        {
            var result = new List<T>();

            // First, lets figure out the bounding box

            var centerTile = DistanceEstimate.Wgs84ToTileNumbers(c, ZoomLevel);
            var nextTile = (centerTile.x + 1, centerTile.y + 1);
            
            var centerXY = DistanceEstimate.NorthWestCoordinateOfTile(centerTile, ZoomLevel);
            var se = DistanceEstimate.NorthWestCoordinateOfTile(nextTile, ZoomLevel);

            
            
            var (firstX, firstY) = DistanceEstimate.Wgs84ToTileNumbers(nw, ZoomLevel);
            var (lastX, lastY) = DistanceEstimate.Wgs84ToTileNumbers(se, ZoomLevel);
            var tiles = _dataPerTile
                .Where(kv =>
                {
                    var x = kv.Key.x;
                    var y = kv.Key.y;
                   return(firstX <= x && x <= lastX && firstY <= y && y <= lastY))
                });
            
            

            // We will need to run from (x - xDiff) to (x + xDiff) to catch all the values
            for (var x = centerTile.x - xDiff; x < centerTile.x + xDiff; x++)
            {
                for (var y = centerTile.y - yDiff; y < centerTile.y + yDiff; y++)
                {
                    var currentTile = (x, y);
                    // Might this tile have values in range? For this, we determine the furthest away corner

                    var furthestX = x;
                    if (x > centerTile.x)
                    {
                        furthestX++;
                    }

                    var furthestY = y;
                    if (y < centerTile.y)
                    {
                        y--;
                    }

                    var furthestCornerCoordinate =
                        DistanceEstimate.NorthWestCoordinateOfTile((furthestX, furthestY), ZoomLevel);
                    var furthestDistance = DistanceEstimate.DistanceEstimateInMeter(c, furthestCornerCoordinate);
                    if (furthestDistance > maxDistanceInMeter)
                    {
                        continue;
                    }

                    if (_dataPerTile.TryGetValue(currentTile, out var data))
                    {
                        result.AddRange(data);
                    }
                }
            }

            return result;
        }
    }
}