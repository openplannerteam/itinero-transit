using System;
using System.Linq;

namespace Itinero.Transit.IO.LC.CSA.LocationProviders
{
    internal static class LocationProviderExtension
    {
        public static string GetNameOf(this ILocationProvider locProv, Uri uri)
        {
            return locProv == null
                ? uri.ToString()
                : $"{locProv.GetCoordinateFor(uri).Name} ({uri.Segments.Last()})";
        }
    }
}