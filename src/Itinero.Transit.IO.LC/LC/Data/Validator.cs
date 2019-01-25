using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.CSA.Connections;
using Itinero.Transit.IO.LC.CSA.LocationProviders;

namespace Itinero.Transit.IO.LC.CSA.Data
{

    internal static class TimeTableExtensions
    {
        internal static void Validate(this TimeTable tt, LocationProvider locations, Func<Connection, Uri, bool> locationNotFound, Func<Connection, bool> duplicateConnection, Func<Connection, string, bool> invalidConnection)
        {
            var validator = new Validator(tt, locations, locationNotFound, duplicateConnection, invalidConnection);
            validator.Validate();
        }
    }
    
    /// <summary>
    /// The validator checks each connection for a few simple things:
    ///
    /// * Invalid Connections, such as no start or end location given, arrival time before departure time, ...
    /// * Connections appearing multiple times
    /// * Unknown locations
    ///
    /// For all of these, an action can be given
    /// </summary>
    internal class Validator
    {
        private readonly TimeTable _connections;
        private readonly LocationProvider _locations;
        private readonly Func<Connection, Uri, bool> _locationNotFound;
        private readonly Func<Connection, bool> _duplicateConnection;
        private readonly Func<Connection, string, bool> _invalidConnection;

        /// <summary>
        /// Construct a validator.
        /// </summary>
        /// <param name="connections">The connections to validate</param>
        /// <param name="locations">The locations to validate against</param>
        /// <param name="locationNotFound">What should happen if a location was not found. Default: throw exception. Should return 'true' if the connection should be retained</param>
        /// <param name="duplicateConnection">What should happen if a connection appears twice. Default: drop in silence. Should return 'true' if the connection should be retained</param>
        /// <param name="invalidConnection">What should happen if a connections is invalid (such as location is null or arrival time before departure time). Default: exception. Should return 'true' if the connection should be retained</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        public Validator(TimeTable connections, LocationProvider locations,
            Func<Connection, Uri, bool> locationNotFound = null,
            Func<Connection, bool> duplicateConnection = null,
            Func<Connection, string, bool> invalidConnection = null)
        {
            _connections = connections;
            _locations = locations;


            _locationNotFound = locationNotFound ?? ((c, uri) =>
                                    throw new ArgumentException(
                                        $"Unkown location URI {uri}\n" +
                                        $"The specified location was not listed. Perhaps a new station or stop was added and the server should be updated?\n" +
                                        $"The connection using this location is{c}"));


            _duplicateConnection = duplicateConnection ?? (c => { return false; });


            _invalidConnection = invalidConnection ?? ((c, msg) =>
                                     throw new ArgumentException($"Invalid connection: {msg}\n{c}"));


            // Duplicate entries
        }

        /// <summary>
        /// Validate the entire timetable
        /// </summary>
        public void Validate()
        {
            var alreadySeen = new HashSet<Connection>();
            var cons = _connections.Connections();
            for (int i = 0; i < cons.Count; i++)
            {
                var connection = cons[i];
                var keepInList = Validate(connection);
                if (alreadySeen.Contains(connection))
                {
                    keepInList &= _duplicateConnection.Invoke(connection);
                }

                if (!keepInList)
                {
                    cons.RemoveAt(i);
                    i--;
                }

                alreadySeen.Add(connection);
            }
        }


        /// <summary>
        /// Default checks on a connection
        /// </summary>
        /// <param name="c"></param>
        private bool Validate(Connection c)
        {
            bool keepInList = true;
            if (c.ArrivalLocation().Equals(c.DepartureLocation()))
            {
                keepInList &= _invalidConnection.Invoke(c,
                    $"This connection ends where it starts, namely at {c.ArrivalLocation()}\n{c.Id()}");
            }

            if (c.ArrivalTime() < c.DepartureTime())
            {
                // We allow arrivalTime to equal Departure time, sometimes buses have less then a minute to travel
                // If there is still to much time difference, the train was probably cancelled, so we throw it out.
                keepInList &= _invalidConnection.Invoke(c,
                    $"WTF? Time travellers! {c.DepartureTime()} (including delays) --> {c.ArrivalTime()} (including delay)");
            }

            if (c.DepartureLocation() == null || c.ArrivalLocation() == null)
            {
                keepInList &= _invalidConnection.Invoke(c, "_departureStop or _arrivalStop is null in the JSON");
            }


            if (_locations.GetCoordinateFor(c.DepartureLocation()) == null)
            {
                keepInList &= _locationNotFound.Invoke(c, c.DepartureLocation());
            }

            if (_locations.GetCoordinateFor(c.ArrivalLocation()) == null)
            {
                keepInList &= _locationNotFound.Invoke(c, c.ArrivalLocation());
            }

            return keepInList;
        }
    }
}