using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Data.Aggregators;
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
            : this(databaseId, new TransitDbSnapShot(
                databaseId,
                "",
                new SimpleStopsDb(databaseId),
                new SimpleConnectionsDb(databaseId),
                new SimpleTripsDb(databaseId)
            ))
        {
        }

        private TransitDb(uint databaseId, TransitDbSnapShot snapshot)
        {
            DatabaseId = databaseId;
            Latest = snapshot;
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

        /// <summary>
        /// This is intended only to be used by Itinero Transit Processor
        /// </summary>
        public static TransitDb CreateMergedTransitDb(IEnumerable<TransitDbSnapShot> tdbs)
        {
            tdbs = tdbs.ToList();

            if (tdbs.Count() == 1)
            {
                return new TransitDb(0, tdbs.First());
            }
            
            var snapshot = new TransitDbSnapShot(0,
                string.Join(";", tdbs.Select(tdb => tdb.GlobalId)),
                StopsDbAggregator.CreateFrom(tdbs),
                ConnectionsDbAggregator.CreateFrom(tdbs),
                TripsDbAggregator.CreateFrom(tdbs)
            );
            return new TransitDb(0, snapshot);
        }
    }
}