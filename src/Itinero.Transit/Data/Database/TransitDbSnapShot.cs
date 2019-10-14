using System;
using System.IO;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A transit db snapshot, represents a consistent state of the transit db.
    /// </summary>
    public class TransitDbSnapShot
    {
        internal TransitDbSnapShot(StopsDb stopsDb, TripsDb tripsDb, IConnectionsDb connectionsDb)
        {
            StopsDb = stopsDb;
            TripsDb = tripsDb;
            ConnectionsDb = connectionsDb;
            Id = stopsDb.DatabaseId;
        }

        public uint Id { get; }

        /// <summary>
        /// Gets the stops db.
        /// </summary>
        public StopsDb StopsDb { get; }

        /// <summary>
        /// Gets the trips db.
        /// </summary>
        public TripsDb TripsDb { get; }

        /// <summary>
        /// Gets the connections db.
        /// </summary>
        public IConnectionsDb ConnectionsDb { get; }

        /// <summary>
        /// Copies this transit db to the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The length of the data written.</returns>
        public long WriteTo(Stream stream)
        {
            var length = 1L;

            byte version = 1;
            stream.WriteByte(version);

            length += StopsDb.WriteTo(stream);
            length += TripsDb.WriteTo(stream);
            length += ConnectionsDb.WriteTo(stream);

            return length;
        }

        public void WriteTo(String filePath)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                WriteTo(stream);
            }
        }
    }
}