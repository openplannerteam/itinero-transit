using System;

namespace Itinero.Transit.IO.LC.IO.LC.Synchronization
{
    public interface SynchronizationPolicy
    {
        uint Frequency { get; }

        void Run(DateTime triggerDate, TransitDbUpdater db);
    }
}