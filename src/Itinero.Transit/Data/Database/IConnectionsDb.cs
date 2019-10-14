using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface IConnectionsDb : IDatabase<ConnectionId, Connection>
    {
        ulong EarliestDate { get; }
        ulong LatestDate { get; }
        ConnectionsDb.DepartureEnumerator GetDepartureEnumerator();
    }
}