using System;
using System.Diagnostics.Contracts;
using System.IO;
using Itinero.Transit.Data.Simple;

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


        public TransitDb(uint databaseId)
        {
            DatabaseId = databaseId;
            Latest = new TransitDbSnapShot(
                databaseId,
                new SimpleStopsDb(databaseId),
                new SimpleConnectionsDb(databaseId),
                new SimpleTripsDb(databaseId)
            );
        }

        public TransitDb(uint databaseId, Stream readFromFile)
        {
            throw new NotImplementedException();
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
    }
}