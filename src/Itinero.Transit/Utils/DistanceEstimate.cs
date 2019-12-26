using System;

namespace Itinero.Transit.Utils
{
    public static class DistanceEstimate
    {
        private const double RadiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// Stolen from https://github.com/itinero/routing/blob/1764afc75db43a1459789592de175283f642123f/src/Itinero/LocalGeo/Coordinate.cs
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        public static float DistanceEstimateInMeter((double lon,double lat) c1,
            (double lon,  double lat) c2)
        {
            var lat1Rad = c1.lat / 180d * Math.PI;
            var lon1Rad = c1.lon / 180d * Math.PI;
            var lat2Rad = c2.lat / 180d * Math.PI;
            var lon2Rad = c2.lon / 180d * Math.PI;

            var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = Math.Sqrt(x * x + y * y) * RadiusOfEarth;

            return (float) m;
        }

        /// <summary>
        /// Gives a new coordinate when moving north for 'meters'
        /// </summary>
        public static (double lon, double lat) MoveNorth((double lon, double lat) c, double meters)
        {
            var dLat = -meters / RadiusOfEarth;
            return (c.lon, c.lat + dLat * 180 / Math.PI);
        }
        
        /// <summary>
        /// Gives a new coordinate when moving east for 'meters'
        /// </summary>
        public static (double lon, double lat) MoveEast((double lon, double lat) c, double meters)
        {
            var dLon = meters/(RadiusOfEarth*Math.Cos(Math.PI*c.lat/180));
            return (c.lon + dLon * 180 / Math.PI, c.lat);
        }
        
        /// <summary>
        /// Get the indexing based on tile number.
        /// Follows the OSM-slippy map scheme.
        /// See https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#X_and_Y
        /// </summary>
        /// <returns>(x,y)-tile number </returns>
        public static (int x, int y) Wgs84ToTileNumbers((double lon, double lat) c, uint zoomLevel)
        {
            var n = Math.Pow(2, zoomLevel);
            var xtile = n * ((c.lon + 180) / 360);
            var latRad = Math.PI * c.lat / 180;
            var ytile = n * (1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2;
            return ((int) xtile, (int) ytile);
        }

        /// <summary>
        /// Returns the North-West upper coordinate of the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="zoomlevel"></param>
        /// <returns></returns>
        public static (double lon, double lat) NorthWestCoordinateOfTile((int x, int y) tile, uint zoomlevel)
        {
            var n = Math.Pow(2, zoomlevel);
            var lonDeg = (tile.x / n) * 360.0 - 180.0;
            var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tile.y / n)));
            var latDeg = 180 * latRad / Math.PI;
            return (latDeg, lonDeg);
        }

        /// <summary>
        /// Calculates the width and height of the given tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="zoomlevel"></param>
        /// <returns></returns>
        public static (double width, double height) SizeOf((int x, int y) tile, uint zoomlevel)
        {
            var nw = NorthWestCoordinateOfTile(tile, zoomlevel);
            var se = NorthWestCoordinateOfTile((tile.x + 1, tile.y - 1), zoomlevel);
            var width = DistanceEstimateInMeter(nw, (se.lon, nw.lat));
            var height = DistanceEstimateInMeter(nw, (nw.lon, se.lat));
            return (width, height);

        }
    }
}