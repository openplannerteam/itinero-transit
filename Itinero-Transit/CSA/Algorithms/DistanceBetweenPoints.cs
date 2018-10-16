using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    ///  Small utility to calculate the length in meters between two coordinates
    /// TODO move upstream, e.g. to Itinero/routing
    /// </summary>
    public class DistanceBetweenPoints
    {
        public static float DistanceInMeters(float lat, float lon, float lat0, float lon0)
        {
            // Shamelessly copied from https://stackoverflow.com/questions/6366408/calculating-distance-between-two-latitude-and-longitude-geocoordinates#6366657
            var baseRad = Math.PI * lat / 180;
            var targetRad = Math.PI * lat0 / 180;
            var theta = lon - lon0;
            var thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            // Distance in degrees
            dist = dist * 180 / Math.PI;
            // distance in  nautical miles
            dist = dist * 60;
            // and in meters
            dist = dist * 1852;

            return (float) dist;
        }
    }
}