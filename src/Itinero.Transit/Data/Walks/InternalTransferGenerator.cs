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
        /// <param name="conn"></param>
        /// <param name="timeNearTransfer">The departure time in the normal case, the arrival time if building journeys from en </param>
        /// <param name="timeNearHead"></param>
        /// <param name="locationNearHead"></param>
        /// <param name="tripId"></param>
        /// <returns></returns>
        private Journey<T> CreateInternalTransfer<T>(Journey<T> buildOn, uint conn, ulong timeNearTransfer,
            ulong timeNearHead, (uint localTileId, uint localId) locationNearHead, uint tripId) where T : IJourneyStats<T>
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

            return buildOn.Transfer(conn, timeNearTransfer, timeNearHead, locationNearHead, tripId);
        }

        public Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, IConnection c) where T : IJourneyStats<T>
        {
            if (c.DepartureTime < buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave departs before the journey arrives. Are you building backward routes? Use the other method (CreateArrivingTransfer)");
            }

            if (c.DepartureStop != buildOn.Location)
            {
                return null;
            }

            return CreateInternalTransfer(buildOn,
                c.Id, c.DepartureTime, c.ArrivalTime, c.ArrivalStop, c.TripId);
        }

        public Journey<T> CreateArrivingTransfer<T>(Journey<T> buildOn, IConnection c) where T : IJourneyStats<T>
        {
            if (c.ArrivalTime > buildOn.Time)
            {
                throw new ArgumentException(
                    "Seems like the connection you gave arrives after the rest journey departs. Are you building forward routes? Use the other method (CreateDepartingTransfer)");
            }

            if (c.ArrivalStop != buildOn.Location)
            {
                return null;
            }

            return CreateInternalTransfer(buildOn, c.Id,
                c.ArrivalTime, c.DepartureTime, c.DepartureStop, c.TripId);
        }
    }
}