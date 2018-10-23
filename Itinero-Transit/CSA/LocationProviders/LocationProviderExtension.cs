using System;

namespace Itinero_Transit.CSA.LocationProviders
{
    public static class LocationProviderExtension
    {
        public static string GetNameOf(this ILocationProvider locProv, Uri uri)
        {
            if (locProv == null)
            {
                return uri.ToString();
            }

            return locProv.GetCoordinateFor(uri).Name;
        }
    }
}