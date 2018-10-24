using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;

namespace Itinero_Transit.CSA.LocationProviders
{
    public class OsmLocationMapping : ILocationProvider
    {
        private static readonly BoundingBox All = new BoundingBox(-90, 90, -180, 180);
        public static readonly OsmLocationMapping Singleton = new OsmLocationMapping();
        
        public Location GetCoordinateFor(Uri locationId)
        {
            var coor = locationId.Fragment.Split("/");
            var lat = float.Parse(coor[1]);
            var lon = float.Parse(coor[2]);
            return new Location(locationId)
            {
                Lat = lat,
                Lon = lon,
                Name = $"{lat},{lon}"
            };
        }

        public bool ContainsLocation(Uri locationId)
        {
            return locationId.ToString().StartsWith("https://www.openstreetmap.org/#map=");
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            return new HashSet<Uri>();
        }

        public BoundingBox BBox()
        {
            return All;
        }

        public IEnumerable<Location> GetLocationByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}