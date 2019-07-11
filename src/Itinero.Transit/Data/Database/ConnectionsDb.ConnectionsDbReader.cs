using System.Collections.Generic;

namespace Itinero.Transit.Data
{
    public partial class ConnectionsDb
    {
        /// <summary>
        /// A connections DB reader is an object which allows accessing properties of a single connection contained in the DB
        /// </summary>
        public class ConnectionsDbReader :
            IDatabaseReader<ConnectionId, SimpleConnection>,
            IDatabaseEnumerator<ConnectionId, SimpleConnection>
        {
            private readonly ConnectionsDb _db;

            internal ConnectionsDbReader(ConnectionsDb db)
            {
                _db = db;
                DatabaseIds = new[] {_db.DatabaseId};
            }

            public bool Get(uint internalId, SimpleConnection objectToWrite)
            {
                return _db.GetConnection(internalId, objectToWrite);
            }

            public bool Get(ConnectionId id, SimpleConnection objectToWrite)
            {
                if (id.DatabaseId != _db.DatabaseId)
                {
                    return false;
                }

                return Get(id.InternalId, objectToWrite);
            }

            public bool Get(string globalId, SimpleConnection objectToWrite)
            {
                var hash = Hash(globalId);
                var pointer = _db._globalIdPointersPerHash[hash];
                while (pointer != _noData)
                {
                    var internalId = _db._globalIdLinkedList[pointer + 0];
                    if (Get(internalId, objectToWrite))
                    {
                        // This could be made more efficient by not relying on Get
                        // But for now, it is fast and even more important: easy and maintainalbe

                        var potentialMatch = objectToWrite.GlobalId;
                        if (potentialMatch == globalId)
                        {
                            return true;
                        }
                    }

                    pointer = _db._globalIdLinkedList[pointer + 1];
                }

                return false;
            }


            public IEnumerable<uint> DatabaseIds { get; }

            public ConnectionId? First()
            {
                if (_db._nextInternalId == 0)
                {
                    return null;
                }

                return new ConnectionId(_db.DatabaseId, 0);
            }

            public bool HasNext(ConnectionId current, SimpleConnection nextToWrite)
            {
                return Get(new ConnectionId(current.DatabaseId, current.InternalId + 1), nextToWrite);
            }
        }
    }
}