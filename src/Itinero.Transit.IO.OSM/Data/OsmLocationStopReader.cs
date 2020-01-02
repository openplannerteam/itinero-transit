using System;
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
        public readonly List<Stop> SearchableLocations = new List<Stop>();


        private const uint Precision = 1000000;

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
            : this(databaseId, searchableLocations?.Select(c => 
                CreateOsmStop(((long) (c.lon * Precision), (long) (c.lat * Precision)))))
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
                    _locationIndex.Add(searchableLocation.Longitude, searchableLocation.Latitude,
                        searchableLocation);
                    SearchableLocations.Add(searchableLocation);
                }
            }
        }


        private static Stop CreateOsmStop((long lon, long lat) location)
        {
            var (lonRounded, latRounded) = location;

            var lonBeforeDot = lonRounded / Precision;
            var lonAfterDot =
                Math.Abs(lonRounded %
                         Precision); // We don't need a minus sign in the url, the lonBeforeDot handles that

            var latBeforeDot = latRounded / Precision;
            var latAfterDot = Math.Abs(latRounded % Precision);
            var globalId =
                $"https://www.openstreetmap.org/#map=19/{latBeforeDot}.{latAfterDot:000000}/{lonBeforeDot}.{lonAfterDot:000000}";


            return new Stop(globalId, ((double) lonRounded / Precision, (double) latRounded / Precision));
        }

        public StopId SearchId((double lon, double lat) c)
        {
            // Range: (0 -> 360 * Precision). Should be moved log(10, Precision) + 3 digits to the left to neatly fit into the long
            var lonRounded = (int) (c.lon * Precision);
            // Range: 0 -> 180 * Precision
            var latRounded = (int) (c.lat * Precision);


            return SearchId(lonRounded, latRounded);
        }

        public StopId SearchId(long lonRounded, long latRounded)
        {
            lonRounded += 180 * Precision;
            latRounded += 90 * Precision;

            return new StopId(_databaseId,
                (ulong) (lonRounded * Precision * 1000 + latRounded));
        }

        public StopId SearchId(string globalId)
        {
            if (!SearchId(globalId, out var id))
            {
                throw new ArgumentException("Could not parse the globalId");
            }

            return id;
        }


        public bool TryGet(StopId id, out Stop t)
        {
            if (_databaseId != id.DatabaseId)
            {
                t = null;
                return false;
            }

            // First we divide, then we cast to double to prevent the latitude from leaking
            // ReSharper disable once PossibleLossOfFraction
            var lonRounded = id.LocalId / (Precision * 1000);
            var latRounded = id.LocalId % (Precision * 1000);

            t = CreateOsmStop(((long) lonRounded -  Precision * 180, (long) latRounded - Precision * 90));
            return true;
        }

        public bool SearchId(string globalId, out StopId id)
        {
            var couldParse = ParseOsmUrl.ParseUrl(Precision).TryParseFull(globalId, out var c, out _);
            if (!couldParse)
            {
                id = StopId.Invalid;
                return false;
            }

            id = SearchId(c.lon, c.lat);
            return true;
        }


        public void PostProcess(uint zoomLevel)
        {
        }

        public IStopsDb Clone()
        {
            return new OsmLocationStopReader(_databaseId, SearchableLocations);
        }

        public IEnumerator<Stop> GetEnumerator()
        {
            return SearchableLocations.GetEnumerator();
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

        public static RDParser<(long lon, long lat)> ParseUrl(uint precision)
        {
            return RDParser<(double lat, double lon)>.X(
                !ParsePrefix() * DoubleAsLong(precision),
                !Lit("/") *
                DoubleAsLong(precision)
            ).Map(c => (c.Item2, c.Item1));
        }
    }
}