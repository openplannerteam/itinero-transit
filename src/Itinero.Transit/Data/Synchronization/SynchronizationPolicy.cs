using System;

namespace Itinero.Transit.Data.Synchronization
{
    public interface ISynchronizationPolicy
    {
        uint Frequency { get; }

        void Run(DateTime triggerDate, TransitDbUpdater db);
    }
}