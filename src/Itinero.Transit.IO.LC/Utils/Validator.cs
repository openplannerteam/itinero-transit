using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.LC.Utils
{
    internal static class TimeTableExtensions
    {
        internal static void Validate(this TimeTable tt, LocationFragment locations)
        {
            var validator = new Validator(tt, locations);
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
    /// When one is encountered for a single connection, an exception is raised
    /// </summary>
    internal class Validator
    {
        private readonly TimeTable _connections;
        private readonly LocationFragment _locations;

        public Validator(TimeTable connections, LocationFragment locations)
        {
            _connections = connections;
            _locations = locations;
        }

        /// <summary>
        /// Validate the entire timetable
        /// </summary>
        public void Validate()
        {
            var alreadySeen = new HashSet<Connection>();
            var cons = _connections.Connections();
            for (var i = 0; i < cons.Count; i++)
            {
                var connection = cons[i];
                try
                {
                    Validate(connection);
                }
                catch (Exception e)
                {
                    alreadySeen.Add(connection); // Implies removal of this connection
                    Log.Error(e.Message);
                }

                if (alreadySeen.Contains(connection))
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
        private void Validate(Connection c)
        {
            if (c.ArrivalLocation().Equals(c.DepartureLocation()))
            {
                throw new ArgumentException(
                    $"Connection {c.Uri} ends where it starts, namely at {c.ArrivalLocation()}");
            }


            if (_locations.GetCoordinateFor(c.DepartureLocation()) == null)
            {
                throw new ArgumentException(
                    $"Unknown departure location URI {c.DepartureLocation()} in connection {c.Uri}");
            }

            if (_locations.GetCoordinateFor(c.ArrivalLocation()) == null)
            {
                throw new ArgumentException(
                    $"Unknown arrival location URI {c.ArrivalLocation()} in connection {c.Uri}");
            }

            if (c.DepartureLocation() == null || c.ArrivalLocation() == null)
            {
                throw new ArgumentException(
                    $"This connection does not have a departure- and arrival location set\n{c.Uri}{c}");
            }
        }
    }
}