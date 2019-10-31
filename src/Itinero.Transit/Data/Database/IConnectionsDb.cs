using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface IConnectionsDb : IDatabase<ConnectionId, Connection>
    {
        /// <summary>
        /// The earliest date loaded into this DB
        /// </summary>
        ulong EarliestDate { get; }

        /// <summary>
        /// The latest date loaded into this DB
        /// </summary>
        ulong LatestDate { get; }

        ConnectionsDb.DepartureEnumerator GetDepartureEnumerator();
    }
}