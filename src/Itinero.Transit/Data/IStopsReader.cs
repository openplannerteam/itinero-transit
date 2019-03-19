namespace Itinero.Transit.Data
{
    public interface IStopsReader : IStop
    {
        bool MoveTo((uint localTileId, uint localId) stop);
        bool MoveTo(string globalId);
        void Reset();

        /// <summary>
        /// Gives the internal StopsDb.
        /// Escapes the abstraction, should only be used for internal operations
        /// </summary>
        /// <returns></returns>
        StopsDb StopsDb { get; }
    }
}