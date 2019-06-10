using System;
using System.IO;

namespace Itinero.Transit.Data.Synchronization
{
    /// <summary>
    /// Saves the database to the disk every now and then
    /// </summary>
    public class WriteToDisk : ISynchronizationPolicy
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
            Directory.GetParent(_saveTo).Create();
            
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