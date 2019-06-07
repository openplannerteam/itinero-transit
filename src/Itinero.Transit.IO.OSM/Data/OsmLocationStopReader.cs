using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.OSM.Data.OpeningHours;
using Itinero.Transit.Utils;
using static Itinero.Transit.IO.OSM.Data.OpeningHours.DefaultRdParsers;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.IO.OSM.Data
{
    /// <summary>
    /// AN 'OsmLocationStop' is used to represent various floating locations.
    /// As global IDs, we reuse the mapping url:
    /// https://www.openstreetmap.org/#map=[irrelevant_zoom_level]/[lat]/[lon]
    /// E.g.:  https://www.openstreetmap.org/#map=19/51.21575/3.21999
    /// </summary>
    public class OsmLocationStopReader : IStopsReader
    {
        private readonly uint _databaseId;
        public string GlobalId { get; private set; }
        public LocationId Id { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public IAttributeCollection Attributes => null; //No attributes supported here

        private const uint _precision = 1000000;

        public OsmLocationStopReader(uint databaseId)
        {
            _databaseId = databaseId;
        }

        public bool MoveTo(string globalId)
        {
            var (lat, lon) =
                ParseOsmUrl.ParseURL().ParseFull(globalId);
            // Slight abuse of the LocationId
            Id = new LocationId(_databaseId, (uint) ((lat + 90.0) * _precision), (uint) ((lon + 180) * _precision));
            Latitude = (double) Id.LocalTileId / _precision - 90.0;
            Longitude = (double) Id.LocalId / _precision - 180.0;
            GlobalId = $"https://www.openstreetmap.org/#map=19/{Latitude}/{Longitude}";
            return true;
        }

        public bool MoveTo(LocationId stop)
        {
            if (stop.DatabaseId != _databaseId)
            {
                return false;
            }

            Latitude = (double) stop.LocalTileId / _precision - 90.0;
            Longitude = (double) stop.LocalId / _precision - 180.0;
            GlobalId = $"https://www.openstreetmap.org/#map=19/{Latitude}/{Longitude}";
            Id = stop;
            return true;
        }

        public bool MoveNext()
        {
            return false;
        }


        public void Reset()
        {
            // Do nothing     
        }

        public float CalculateDistanceBetween(LocationId a, LocationId b)
        {
            MoveTo(a);
            var lat0 = Latitude;
            var lon0 = Longitude;
            MoveTo(b);
            var lat1 = Latitude;
            var lon1 = Longitude;
            return DistanceEstimate.DistanceEstimateInMeter(lat0, lon0, lat1, lon1);
        }

        public IEnumerable<IStop> LocationsInRange(double lat, double lon, double range)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            throw new NotImplementedException();
        }

        public IStop SearchClosest(double lon, double lat, double maxDistanceInMeters = 1000)
        {
            throw new NotImplementedException();
        }
    }

    internal static class ParseOsmUrl
    {
        internal static RDParser<int> ParsePrefix()
        {
            return !(LitCI("https") | LitCI("http"))
                   * !Lit("://")
                   * !(LitCI("www.") | Lit(""))
                   * !(LitCI("openstreetmap.org") | LitCI("osm.org") | LitCI("openstreetmap.com"))
                   * !LitCI("/#map=")
                   * Int()
                   + !Lit("/");
        }

        public static RDParser<(double, double)> ParseURL()
        {
            return RDParser<(double latitude, double longitude)>.X(
                !ParsePrefix() * Double(),
                !Lit("/") *
                Double()
            );
        }
    }
}