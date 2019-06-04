using System;
using System.Collections.Generic;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Does all kinds of weird validation
    /// </summary>
    public static class Validation
    {
        public static void CheckTripContinuity(this TransitDb.TransitDbSnapShot tdb, DateTime start, DateTime end)
        {
            // Maps where the trip currently is
            var currLocations = new Dictionary<TripId, (LocationId, ulong)>();
            var trip = tdb.TripsDb.GetReader();
            var conn = tdb.ConnectionsDb.GetDepartureEnumerator();
            conn.MoveNext(start);
            while (conn.MoveNext() && conn.DepartureTime <= end.ToUnixTime())
            {
                var tripId = conn.TripId;
                if (!currLocations.ContainsKey(tripId))
                {
                    // We found the start of this trip
                    currLocations[tripId] = (conn.ArrivalStop, conn.ArrivalTime);
                }
                else
                {
                    var (prevLoc, prevTime) = currLocations[tripId];
                    if (!prevLoc.Equals(conn.DepartureStop))
                    {
                        trip.MoveTo(conn.TripId);
                        throw new ArgumentException(
                            $"Error in trip {trip.GlobalId}: the trip makes a jump." +
                            $" Was previously at {prevLoc} but now at {conn.DepartureStop}");
                    }

                    if (prevTime > conn.DepartureTime)
                    {
                        trip.MoveTo(conn.TripId);
                        throw new ArgumentException(
                            $"Error in trip {trip.GlobalId}: the trip continues before it arrived");
                    }
                    
                }
            }
        }
    }
}