using System;
using System.Collections.Generic;
using Itinero.Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero.Transit.CSA.Data;
using JsonLD.Core;

namespace Itinero.Transit.CSA.LocationProviders
{
    public class CachedLocationsFragment : ILocationProvider
    {
        private readonly LocationsFragment _frag;

        public CachedLocationsFragment(Uri fragmentLocation, JsonLdProcessor proc, LocalStorage storage)
        {
            if (storage.Contains(fragmentLocation.ToString()))
            {
                _frag = storage.Retrieve<LocationsFragment>(fragmentLocation.ToString());
            }
            else
            {
                _frag = new LocationsFragment(fragmentLocation);
                _frag.Download(proc);
                storage.Store(fragmentLocation.ToString(), _frag);
            }
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            return _frag.GetCoordinateFor(locationId);
        }

        public bool ContainsLocation(Uri locationId)
        {
            return _frag.ContainsLocation(locationId);
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            return _frag.GetLocationsCloseTo(lat, lon, radiusInMeters);
        }

        public BoundingBox BBox()
        {
            return _frag.BBox();
        }

        public IEnumerable<Location> GetLocationByName(string name)
        {
            return _frag.GetLocationByName(name);
        }
    }
}