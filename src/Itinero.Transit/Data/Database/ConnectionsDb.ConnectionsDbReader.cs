using System.Diagnostics.Contracts;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A connections DB reader is an object which allows accessing properties of a single connection contained in the DB
    /// </summary>
    public partial class ConnectionsDb :
        IDatabaseReader<ConnectionId, Connection>,
        IDatabaseEnumerator<ConnectionId>
    {
        public bool Get(ConnectionId id, Connection objectToWrite)
        {
            return GetConnection(id, objectToWrite);
        }

        public bool Get(string globalId, Connection objectToWrite)
        {
            var hash = Hash(globalId);
            var pointer = _globalIdPointersPerHash[hash];
            while (pointer != _noData)
            {
                var internalId = GlobalIdLinkedList[pointer + 0];
                if (Get(new ConnectionId(DatabaseId, internalId), objectToWrite))
                {
                    // This could be made more efficient by not relying on Get
                    // But for now, it is fast and even more important: easy and maintainalbe

                    var potentialMatch = objectToWrite.GlobalId;
                    if (potentialMatch == globalId)
                    {
                        return true;
                    }
                }

                pointer = GlobalIdLinkedList[pointer + 1];
            }

            return false;
        }


        [Pure]
        public ConnectionId? First()
        {
            if (_nextInternalId == 0)
            {
                return null;
            }

            return new ConnectionId(DatabaseId, 0);
        }

        [Pure]
        public bool HasNext(ConnectionId current, out ConnectionId next)
        {
            next = new ConnectionId(current.DatabaseId, current.InternalId + 1);
            return next.InternalId < _nextInternalId;
        }
    }
}