using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Validator
{
    public class ValidateTrips : IValidation
    {
        public List<Message> Validate(TransitDb tdb)
        {
            var errors = new List<Message>();


            var currentTripCoordinates = new Dictionary<TripId, Connection>();

            var conns = tdb.Latest.ConnectionsDb;
            var enumerator = conns.GetDepartureEnumerator();
            enumerator.MoveTo(conns.EarliestDate);

            void Emit(string type, string message)
            {
                var conn = new Connection();
                enumerator.Current(conn);
                errors.Add(new Message
                {
                    Connection = conn,
                    MessageText = message,
                    Type = type
                });
            }


            var stops = tdb.Latest.StopsDb.GetReader();


            while (enumerator.MoveNext())
            {
                var c = new Connection();
                enumerator.Current(c);

                if (currentTripCoordinates.TryGetValue(c.TripId, out var oldConnection))
                {
                    stops.MoveTo(oldConnection.ArrivalStop);
                    var prevStop = new Stop(stops);
                    stops.MoveTo(c.DepartureStop);
                    var currStop = new Stop(stops);
                    stops.MoveTo(c.ArrivalStop);
                    var nextStop = new Stop(stops);
                    var distance = DistanceEstimate.DistanceEstimateInMeter(currStop.Latitude, currStop.Longitude,
                        nextStop.Latitude, nextStop.Longitude);
                    var speedMS = distance / (c.ArrivalTime - c.DepartureTime);
                    var speedKmH = speedMS * 6 * 6 / 10;


                    if (!oldConnection.ArrivalStop.Equals(c.DepartureStop))
                    {
                        Emit("jump",
                            $"The trip jumps: the previous location of this connection was {prevStop.GlobalId} {prevStop.GetName()} but the current departure location is {currStop.GlobalId} {currStop.GetName()}");
                    }

                    if (c.DepartureStop.Equals(c.ArrivalStop))
                    {
                        Emit("stall",
                            $"This train stays in the same place");
                    }

                    if (c.ArrivalTime < c.DepartureTime)
                    {
                        Emit("timetravel between station",
                            $"This train travels back in time, it arrives {c.DepartureTime - c.ArrivalTime}s before it departs");
                    }


                    if (oldConnection.ArrivalTime > c.DepartureTime)
                    {
                        var delta = oldConnection.ArrivalTime - c.DepartureTime;
                        Emit("timetravel in station",
                            $"Connection departs {delta} seconds before its arrival. Departure time is {c.DepartureTime}, arrival time is {oldConnection.ArrivalTime} (note: the previous connection has {oldConnection.ArrivalDelay}s delay arriving)");
                    }

                    if (c.ArrivalTime == c.DepartureTime)
                    {
                        Emit("teleportation",
                            "This trip needs no time to reach the next station");
                    }
                    else if (distance < 1)
                    {
                        Emit("Closeby stations",
                            $"These stations are less then 1m apart: {currStop.GlobalId} and {nextStop.GlobalId}");
                    }
                    else
                    {
                        if (speedKmH < 0)
                        {
                            Emit("reverse",
                                "The speed of this connection is negative. Probably a timetraveller as well");
                        }

                        if (speedKmH <= 1.0 && speedKmH >= 0)
                        {
                            Emit("slow", "This vehicle only goes about 1 km/h");
                        }

                        if (speedKmH > 150)
                        {
                            Emit("fast",
                                $"This is a very fast train driving {speedKmH}km/h. (This can be correct in the case of TGV/ICE/...");
                        }

                        if (speedKmH >= 574)
                        {
                            Emit("world-record", $"This train drives {speedKmH}km/h. The current world record is 574");
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