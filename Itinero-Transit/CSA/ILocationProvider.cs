using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// An ILocationProvider is responsible for the conversion of location-URI's into coordinates and
    /// is responsible in finding locations nearby a certain coordinate (to search possible intermodal transfers).
    ///
    /// LocationProviders will often accompany a ConnectionProvider, to map the locations of that provider onto coordinates.
    /// </summary>
    public interface ILocationProvider
    {
        Location GetCoordinateFor(Uri locationId);

        IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters);
    }
}