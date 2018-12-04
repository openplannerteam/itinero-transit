using System;

namespace Itinero.Transit.Data.Walks
{
    /// <summary>
    ///  Generates internal transfers if there is enough time
    /// </summary>
    public class NoWalksGenerator<T> : WalksGenerator<T>
        where T : IJourneyStats<T>
    {
        private readonly uint _internalTransferTime;

        public NoWalksGenerator(uint internalTransferTime = 180)
        {
            _internalTransferTime = internalTransferTime;
        }


        /// <summary>
        /// Creates an internal transfer.
        /// </summary>
        /// <param name="buildOn"></param>
        /// <param name="conn"></param>
        /// <param name="timeNearTransfer">The departure time in the normal case, the arrival time if building journeys from en </param>
        /// <param name="timeNearHead"></param>
        /// <param name="locationNearHead"></param>
        /// <param name="tripId"></param>
        /// <returns></returns>
        private Journey<T> CreateInternalTransfer(Journey<T> buildOn, uint conn, uint timeNearTransfer,
            uint timeNearHead, ulong locationNearHead, uint tripId)
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


            if (timeDiff <= _internalTransferTime)
            {
                return null; // Too little time to transfer
            }

            return buildOn.Transfer(conn, timeNearTransfer, timeNearHead, locationNearHead, tripId);
        }

        public Journey<T> CreateDepartureTransfer(Journey<T> buildOn, uint nextConnection,
            uint connDeparture, ulong connDepartureLoc,
            uint connArr, ulong connArrLoc, uint tripId)
        {
            if (connDeparture < buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave departs before the journey arrives. Are you building backward routes? Use the other method (CreateArrivingTransfer)");
            }

            if (connDepartureLoc != buildOn.Location)
            {
                return null;
            }

            return CreateInternalTransfer(buildOn,
                nextConnection, connDeparture, connArr, connArrLoc, tripId);
        }

        public Journey<T> CreateArrivingTransfer(Journey<T> buildOn, uint nextConnection,
            uint connDeparture, ulong connDepartureLoc,
            uint connArr, ulong connArrLoc, uint tripId)
        {
            if (connArr > buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave arrives after the rest journey departs. Are you building forward routes? Use the other method (CreateDepartingTransfer)");
            }

            if (connArrLoc != buildOn.Location)
            {
                return null;
            }

            return CreateInternalTransfer(buildOn, nextConnection,
                connArr, connDeparture, connDepartureLoc, tripId);
        }
    }
}