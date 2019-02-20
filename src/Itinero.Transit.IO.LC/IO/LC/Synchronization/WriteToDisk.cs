using System;
using System.IO;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Saves the database to the disk every now and then
    /// </summary>
    public class WriteToDisk : SynchronizationPolicy
    {
        private readonly string _saveTo;

        public uint Frequency { get; }

        public WriteToDisk(uint frequency, string saveTo)
        {
            Frequency = frequency;
            _saveTo = saveTo;
        }

        public void Run(DateTime triggerDate, TransitDbUpdater db)
        {
            var tdb = db.TransitDb.Latest;
            using (var stream = File.OpenWrite(_saveTo))
            {
                tdb.WriteTo(stream);
            }
        }

        public override string ToString()
        {
            return $"TransitDB to Disk Writer. Saves to {_saveTo} every {TimeSpan.FromSeconds(Frequency):g}";
        }
    }
}