using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A transit db contains all connections, trips and stops.
    /// </summary>
    public class TransitDb
    {
        /// <summary>
        /// The identifier of the database. Should be unique amongst the program
        /// </summary>
        public uint DatabaseId { get; }

        /// <summary>
        /// The actual data
        /// </summary>
        public TransitDbSnapShot Latest;


        public TransitDb(uint databaseId) : this(databaseId,
            new StopsDb(databaseId), new TripsDb(databaseId), new ConnectionsDb(databaseId))
        {
        }

        private TransitDb(uint databaseId, StopsDb stopsDb, TripsDb tripsDb, ConnectionsDb connectionsDb)
        {
            Latest = new TransitDbSnapShot(stopsDb, tripsDb, connectionsDb);
            DatabaseId = databaseId;
        }

        private readonly object _writerLock = new object();
        private TransitDbWriter _writer;


        /// <summary>
        /// Gets a writer.
        /// A writer can add or update entries in the database.
        /// Once all updates are done, the writer should be closed to apply the changes.
        /// </summary>
        /// <returns>A writer.</returns>
        /// <exception cref="InvalidOperationException">Throws if there is already a writer active.</exception>
        [Pure]
        public TransitDbWriter GetWriter()
        {
            lock (_writerLock)
            {
                if (_writer != null)
                    throw new InvalidOperationException(
                        "There is already a writer active, only one writer per transit db can be active at the same time.");

                _writer = new TransitDbWriter(this, Latest);
                return _writer;
            }
        }

        /// <summary>
        /// This method is called by the writer itself and closely coupled to it
        /// </summary>
        internal void SetSnapshot(TransitDbSnapShot snapShot)
        {
            lock (_writerLock)
            {
                Latest = snapShot;
                _writer = null;
            }
        }


        [Pure]
        public static IEnumerable<TransitDb> ReadFrom(IEnumerable<string> paths)
        {
            var tdbs = new List<TransitDb>();
            var i = 0;
            foreach (var path in paths)
            {
                tdbs.Add(ReadFrom(path, (uint) i));
                i++;
            }

            return tdbs;
        }

        [Pure]
        public static TransitDb ReadFrom(string path, uint databaseId)
        {
            using (var stream = File.OpenRead(path))
            {
                return ReadFrom(stream, databaseId);
            }
        }

        /// <summary>
        /// Reads a transit db an all its data from the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="databaseId"></param>
        /// <returns>The transit db.</returns>
        [Pure]
        public static TransitDb ReadFrom(Stream stream, uint databaseId)
        {
            var version = stream.ReadByte();
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(TransitDb)}, invalid version #.");

            var stopsDb = StopsDb.ReadFrom(stream, databaseId);
            var tripsDb = TripsDb.ReadFrom(stream, databaseId);
            var connectionsDb = ConnectionsDb.ReadFrom(stream, databaseId);

            return new TransitDb(databaseId, stopsDb, tripsDb, connectionsDb);
        }
    }
}