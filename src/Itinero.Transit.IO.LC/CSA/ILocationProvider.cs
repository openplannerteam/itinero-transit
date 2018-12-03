using System;
using System.Collections.Generic;

namespace Itinero.IO.LC
{
    /// <summary>
    /// An ILocationProvider is responsible for the conversion of location-URI's into coordinates and
    /// is responsible in finding locations nearby a certain coordinate (to search possible intermodal transfers).
    /// </summary>
    public interface ILocationProvider
    {
        /// <summary>
        /// Gets the metadata for a given location URI.
        /// Throws an error if the key cannot be found
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        Location GetCoordinateFor(Uri locationId);

        /// <summary>
        /// Checks if the given URI can be decoded to a Location by this provider.
        /// Locations which were returned by 'GetLocationsCloseTo' should always be resolvable by the provider
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        bool ContainsLocation(Uri locationId);

        IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters);

        BoundingBox BBox();
        IEnumerable<Location> GetLocationByName(string name);
    }
}