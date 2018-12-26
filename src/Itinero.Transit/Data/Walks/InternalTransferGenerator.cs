using System;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data.Walks
{
    /// <summary>
    ///  Generates internal (thus within the station) transfers if there is enough time to make the transfer.
    /// Returns null if two different locations are given
    /// </summary>
    public class InternalTransferGenerator : IOtherModeGenerator
    {
        private readonly uint _internalTransferTime;

        public InternalTransferGenerator(uint internalTransferTime = 180)
        {
            _internalTransferTime = internalTransferTime;
        }


        /// <summary>
        /// Creates an internal transfer.
        /// </summary>
        /// <param name="buildOn"></param>
        /// <param name="timeNearTransfer">The departure time in the normal case, the arrival time if building journeys from en </param>
        /// <returns></returns>
        private Journey<T> CreateInternalTransfer<T>(Journey<T> buildOn,
            ulong timeNearTransfer) where T : IJourneyStats<T>
        {
            ulong timeDiff;
            if (timeNearTransfer < buildOn.Time)
            {
                timeDiff = buildOn.Time - timeNearTransfer;
            }
            else
            {
                timeDiff = timeNearTransfer - buildOn.Time;
            }


            if (timeDiff < _internalTransferTime)
            {
                return null; // Too little time to transfer
            }

            return buildOn.Transfer(timeNearTransfer);
        }

        public Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, ulong timeWhenLeaving,
            (uint, uint) otherLocation) where T : IJourneyStats<T>
        {
            if (timeWhenLeaving < buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave departs before the journey arrives. Are you building backward routes? Use the other method (CreateArrivingTransfer)");
            }

            if (buildOn.Location != otherLocation)
            {
                // Internal transfer policy does not take care of different locations
                return null;
            }

            return CreateInternalTransfer(buildOn, timeWhenLeaving);
        }

        public Journey<T> CreateArrivingTransfer<T>(Journey<T> buildOn, ulong timeWhenArriving,
            (uint, uint) otherLocation) where T : IJourneyStats<T>
        {
            if (timeWhenArriving > buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave arrives after the rest of the journey departs. Are you building forward routes? Use the other method (CreateDepartingTransfer)");
            }

            if (otherLocation != buildOn.Location)
            {
                return null;
            }

            return CreateInternalTransfer(buildOn, timeWhenArriving);
        }

        public float Range()
        {
            return 0.0f;
        }
    }
}