using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;
using Itinero.Transit.IO.OSM.Data.Parser;
using static Itinero.Transit.IO.OSM.Data.Parser.DefaultRdParsers;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.IO.OSM.Data
{
    /// <summary>
    /// AN 'OsmLocationStop' is used to represent various floating locations.
    /// As global IDs, we reuse the mapping url:
    /// https://www.openstreetmap.org/#map=[irrelevant_zoom_level]/[lat]/[lon]
    /// E.g.:  https://www.openstreetmap.org/#map=19/51.21575/3.21999
    /// </summary>
    public class OsmLocationStopReader : IStopsDb
    {
        private readonly uint _databaseId;

        private readonly TiledLocationIndexing<Stop> _locationIndex = new TiledLocationIndexing<Stop>();

        public ILocationIndexing<Stop> LocationIndex => _locationIndex;


        public IEnumerable<uint> DatabaseIds { get; }


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
        private readonly List<Stop> _searchableLocations = new List<Stop>();


        private const uint Precision = 10000000;

        /// <summary>
        /// Creates a StopsReader which is capable of decoding OSM-urls.
        ///
        /// While every location can be decoded, the 'searchableLocations' will be given via the LocationIndex and GetEnumerator.
        /// This makes that they can be picked up by queries.
        /// 
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="searchableLocations">Locations that can be picked up by GetEnumerator and SearchClosest</param>
        public OsmLocationStopReader(uint databaseId, IEnumerable<(double lon, double lat)> searchableLocations)
            : this(databaseId, searchableLocations?.Select(CreateOsmStop))
        {
        }

        /// <summary>
        /// Creates a StopsReader which is capable of decoding OSM-urls.
        ///
        /// While every location can be decoded, the 'searchableLocations' will be given via the LocationIndex and GetEnumerator.
        /// This makes that they can be picked up by queries.
        /// 
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="searchableLocations">Locations that can be picked up by GetEnumerator and SearchClosest</param>
        public OsmLocationStopReader(uint databaseId, IEnumerable<Stop> searchableLocations = null)
        {
            _databaseId = databaseId;
            DatabaseIds = new[] {_databaseId};
            // ReSharper disable once InvertIf
            if (searchableLocations != null)
            {
                foreach (var searchableLocation in searchableLocations)
                {
                    _locationIndex.Add(searchableLocation.Longitude, searchableLocation.Latitude, searchableLocation);
                    _searchableLocations.Add(searchableLocation);
                }
            }
        }


        public StopId SearchId((double lon, double lat) c)
        {
            var lonRounded = (int) ((c.lon + 180) * Precision);
            var latRounded = (int) ((c.lat + 90) * Precision);

            return new StopId(_databaseId,
                (uint) ((lonRounded + 180) * 10000 + (latRounded + 90) * 10000));
        }

        private static Stop CreateOsmStop((double lon, double lat) location)
        {
            var (lon, lat) = location;

            var lonRounded = lon * Precision / Precision;
            var latRounded = lat * Precision / Precision;
            var globalId = $"https://www.openstreetmap.org/#map=19/{latRounded}/{lonRounded}";


            return new Stop(globalId, (lonRounded, latRounded));
        }

        public bool TryGet(StopId id, out Stop t)
        {
            if (_databaseId != id.DatabaseId)
            {
                t = null;
                return false;
            }

            var lat = (id.LocalId % 10000) / 1000.0;
            // ReSharper disable once PossibleLossOfFraction
            var lon = (double) (id.LocalId / 1000) / Precision;

            t = CreateOsmStop((lon, lat));
            return true;
        }

        public bool SearchId(string globalId, out StopId id)
        {
            var (lat, lon) = ParseOsmUrl.ParseUrl().ParseFull(globalId);
            id = SearchId((lon, lat));
            return true;
        }


        public void PostProcess(uint zoomLevel)
        {
        }

        public IStopsDb Clone()
        {
            return new OsmLocationStopReader(_databaseId, _searchableLocations);
        }

        public IEnumerator<Stop> GetEnumerator()
        {
            return _searchableLocations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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