using System;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A transit db contains all connections, trips and stops.
    /// </summary>
    public class TransitDb
    {
        /// <summary>
        /// The actual data
        /// </summary>
        public TransitDbSnapShot Latest;


        public TransitDb(uint databaseId)

        {
            Latest = TransitDbSnapShot.CreateSimple(databaseId, "not set").GetSnapshot();
        }


        private readonly object _writerLock = new object();
        private SimpleWriter _writer;


        /// <summary>
        /// Gets a writer.
        /// A writer can add or update entries in the database.
        /// Once all updates are done, the writer should be closed to apply the changes.
        /// </summary>
        /// <returns>A writer.</returns>
        /// <exception cref="InvalidOperationException">Throws if there is already a writer active.</exception>
        [Pure]
        public IWriter GetWriter()
        {
            lock (_writerLock)
            {
                if (_writer != null)
                    throw new InvalidOperationException(
                        "There is already a writer active, only one writer per transit db can be active at the same time.");

                return Latest.Edit();
            }
        }

        /// <summary>
        /// This method is called by the writer itself and closely coupled to it
        /// </summary>
        public void CloseWriter()
        {
            lock (_writerLock)
            {
                Latest = _writer.GetSnapshot();
                _writer = null;
            }
        }
    }
}