using System;

namespace Itinero.Transit.IO.LC.CSA.ConnectionProviders
{
    internal class InternalTransferGenerator : IFootpathTransferGenerator
    {
        private readonly int _secondsToTransferNeeded;

        public InternalTransferGenerator(int secondsToTransferNeeded)
        {
            _secondsToTransferNeeded = secondsToTransferNeeded;
        }

//        public IContinuousConnection GenerateFootPaths(DateTime departureTime,
//            Location from, Location to)
//        {
//            if (!Equals(from, to))
//            {
//                return null;
//            }
//
//            return new InternalTransfer(from.Id(), departureTime,
//                departureTime.AddSeconds(_secondsToTransferNeeded));
//        }
    }
}