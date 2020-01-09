using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Validator
{
    public class ValidateTrips : IValidation
    {
        public List<Message> Validate(TransitDb tdb, bool relax = false)
        {
            var errors = new List<Message>();


            var currentTripCoordinates = new Dictionary<TripId, Connection>();

            var connections = tdb.Latest.ConnectionsDb;

            void Err(Connection conn, string type, string message)
            {
                errors.Add(new Message
                {
                    Connection = conn,
                    MessageText = message,
                    Type = type,
                    IsHardError = true
                });
            }

            void Wrn(Connection conn, string type, string message)
            {
                errors.Add(new Message
                {
                    Connection = conn,
                    MessageText = message,
                    Type = type,
                    IsHardError = false
                });
            }


            var stops = tdb.Latest.StopsDb;
            var trips = tdb.Latest.TripsDb;

            Connection prevConnection = null;
            foreach (var c in connections)
            {
                if (prevConnection != null && prevConnection.DepartureTime > c.DepartureTime)
                {
                    throw new Exception(
                        $"ERROR IN DEPARTURE ENUMERATOR! PANIC PANIC PANIC! {prevConnection.DepartureTime} > {c.DepartureTime}");
                }

                prevConnection = c;

                if (currentTripCoordinates.TryGetValue(c.TripId, out var oldConnection))
                {
                    var prevprevStop = stops.Get(oldConnection.DepartureStop);

                    var prevStop = stops.Get(oldConnection.ArrivalStop);
                    var currStop = stops.Get(c.DepartureStop);
                    var nextStop = stops.Get(c.ArrivalStop);

                    var distance = DistanceEstimate.DistanceEstimateInMeter(
                        (currStop.Longitude, currStop.Latitude),
                        (nextStop.Longitude, nextStop.Latitude));
                    var speedMs = distance / c.TravelTime;
                    var speedKmH = speedMs * 6 * 6 / 10;

                    var trip = trips.Get(c.TripId);

                    var stationInfo =
                        $"{currStop.GlobalId} {currStop.GetName()} and {nextStop.GlobalId} {nextStop.GetName()} (totaldistance {(int) distance})";

                    if (!oldConnection.ArrivalStop.Equals(c.DepartureStop))
                    {
                        Err(c, "jump",
                                $"Error in trip {trip.GlobalId}\n" +
                                $"The trip arrived in {prevStop.GetName()} at {oldConnection.ArrivalTime.FromUnixTime():s} from {prevprevStop.GetName()}\n" +
                                $"The trip now departs in {currStop.GetName()} at {c.DepartureTime.FromUnixTime():s}\n" +
                                $"The trip should arrive in {nextStop.GetName()} at {c.ArrivalTime.FromUnixTime():s}" +
                                $"Previous trip data {prevConnection.ToJson()}" +
                                $"All times include delay (if any delay is present)")
                            ;
                    }

                    if (c.DepartureStop.Equals(c.ArrivalStop))
                    {
                        Err(c, "stall",
                            $"This train stays in the same place");
                    }

                    if (oldConnection.DepartureTime + oldConnection.TravelTime > c.DepartureTime)
                    {
                        var delta = oldConnection.ArrivalTime - c.DepartureTime;
                        var stop = c.DepartureStop;
                        var stopName = stops.Get(stop).GetName() ?? "";
                        
                        Err(c, "timetravel in station",
                            $"Connection departs {delta} seconds before its arrival in {stopName}. Departure time is {c.DepartureTime.FromUnixTime():HH:mm:ss}, arrival time is {oldConnection.ArrivalTime.FromUnixTime():HH:mm:ss}\n" +
                            $"OldConnection data: {oldConnection.ToJson()}");
                    }

                    if (c.ArrivalTime == c.DepartureTime)
                    {
                        if (!relax || distance >= 10000)
                        {
                            Wrn(c, "teleportation",
                                $"This trip needs no time to reach the next station: {stationInfo}");
                        }
                    }
                    else if (
                        (!relax && distance < 50) ||
                        (relax && distance < 1))
                    {
                        Err(c, "Closeby stations",
                            $"These stations are less then 1m apart: {currStop.GlobalId} and {nextStop.GlobalId}");
                    }
                    else
                    {
                        if (speedKmH < 0)
                        {
                            Err(c, "reverse",
                                "The speed of this connection is negative. Probably a timetraveller as well");
                        }

                        if (speedKmH <= 1.0 && speedKmH >= 0 &&
                            !(relax && distance < 10000))
                        {
                            Wrn(c, "slow", $"This vehicle only goes about 1 km/h between {stationInfo}");
                        }


                        if (
                            !(relax && distance < 10000) // Free pass for anything under 10km
                            &&
                            (distance < 10000 && speedKmH > 100 ||
                             distance < 50000 && speedKmH > 150 ||
                             distance < 100000 && speedKmH > 200))

                        {
                            Wrn(c, "fast",
                                $"This is a very fast train driving {speedKmH}km/h in {c.ArrivalTime - c.DepartureTime}s between {stationInfo}. (This can be correct in the case of TGV/ICE/...");
                        }

                        if (speedKmH >= 574)
                        {
                            Err(c, "world-record",
                                $"This train drives {speedKmH}km/h between {stationInfo}. The current world record is 574");
                        }
                    }
                }

                currentTripCoordinates[c.TripId] = c;
            }


            return errors;
        }

        public string About =>
            "Validates that a trip does not violate time constraints, e.g. that, if the train arrives in A, it doesn't leave earlier";

        public string Name =>
            "trip validation";
    }
}