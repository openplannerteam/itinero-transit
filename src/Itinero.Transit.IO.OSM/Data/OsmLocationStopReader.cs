using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.OSM.Data.OpeningHours;
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
        /// <summary>
        /// If we were to search locations close by a given location, we could return infinitely many coordinates.
        ///
        /// However, often we are only interested in finding two locations:
        /// The departure location and the arrival location.
        ///
        /// This list acts as the items that can be returned in 'SearchInBox', searchClosest, etc...
        ///
        /// We expect this list to stay small (at most 100) so we are not gonna optimize this a lot
        /// 
        /// </summary>
        private readonly List<LocationId> _searchableLocations = new List<LocationId>();


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

        public bool MoveTo((double latitude, double longitude) location)
        {
            var (lat, lon) =
                location;
            // Slight abuse of the LocationId
            Id = new LocationId(_databaseId, (uint) ((lat + 90.0) * _precision), (uint) ((lon + 180) * _precision));
            Latitude = (double) Id.LocalTileId / _precision - 90.0;
            Longitude = (double) Id.LocalId / _precision - 180.0;
            GlobalId = $"https://www.openstreetmap.org/#map=19/{Latitude}/{Longitude}";
            return true;
        }

        public bool MoveTo(string globalId)
        {
            var (lat, lon) =
                ParseOsmUrl.ParseUrl().ParseFull(globalId);
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

        /// <summary>
        /// Enumerates the special 'inject locations' list
        /// </summary>
        private int _index;

        public bool MoveNext()
        {
            _index++;
            if (_index >= _searchableLocations.Count)
            {
                return false;
            }

            MoveTo(_searchableLocations[_index]);
            return true;
        }


        public void Reset()
        {
            _index = 0;
        }

        public void AddSearchableLocation(LocationId location)
        {
            _searchableLocations.Add(location);
        }

        public LocationId AddSearchableLocation((double latitude, double longitude) location)
        {
            MoveTo(location);
            AddSearchableLocation(Id);
            return Id;
        }

        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            
            var results=  new List<IStop>();
            foreach (var location in _searchableLocations)
            {
                MoveTo(location);
                if (box.minLon <= Longitude && Longitude <= box.maxLon
                    && box.minLat <= Latitude && Latitude <= box.maxLat)
                {
                    results.Add(new Stop(this));
                }
            }

            return results;
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

        public static RDParser<(double, double)> ParseUrl()
        {
            return RDParser<(double latitude, double longitude)>.X(
                !ParsePrefix() * Double(),
                !Lit("/") *
                Double()
            );
        }
    }
}