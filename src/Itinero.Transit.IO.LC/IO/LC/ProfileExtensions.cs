using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;

namespace Itinero.Transit.IO.LC
{
    public static class ProfileExtensions
    {
        /// <summary>
        /// Adds all connection data between the given dates into the databases.
        /// Locations are only added as needed
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="transitDb"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="onError"></param>
        /// <param name="onLocationHandled">Callback when a location is added</param>
        /// <param name="onTimeTableHandled">Callback when a timetable has been handled</param>
        public static void AddAllConnectionsTo(this Profile profile,
            TransitDb.TransitDbWriter writer,
            DateTime start, DateTime end, Action<string> onError,
            LoggingOptions onLocationHandled = null,
            LoggingOptions onTimeTableHandled = null)
        {
            var dbs = new DatabaseLoader(writer, onLocationHandled, onTimeTableHandled, onError);
            dbs.AddAllLocations(profile);
            dbs.AddAllConnections(profile, start, end);
        }

        /// <summary>
        /// Adds all location data into the database
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="transitDb"></param>
        /// <param name="onError"></param>
        /// <param name="onLocationHandled"></param>
        public static void AddAllLocationsTo(this Profile profile,
            TransitDb.TransitDbWriter writer,
            Action<string> onError,
            LoggingOptions onLocationHandled = null
        )
        {
            var dbs = new DatabaseLoader(writer, onLocationHandled, null, onError);
            dbs.AddAllLocations(profile);
        }
    }
}