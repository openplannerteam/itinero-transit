namespace Itinero.Transit.Data.Tiles
{
    internal static class TiledLocationIndexExtensions
    {
        /// <summary>
        /// Gets a tile range enumerator for the given tile range.
        /// </summary>
        /// <param name="index">The location index.</param>
        /// <param name="tileRange">The tile range.</param>
        /// <returns>A tile range enumerator.</returns>
        public static TileRangeLocationEnumerable GetTileRangeEnumerator(this TiledLocationIndex index, TileRange tileRange)
        {
            return new TileRangeLocationEnumerable(index, tileRange);
        }
    }
}