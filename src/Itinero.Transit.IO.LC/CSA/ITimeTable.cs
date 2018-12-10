using System;
using System.Collections.Generic;

namespace Itinero.Transit.IO.LC.CSA
{
    /// <summary>
    /// A TimeTable is an object containing multiple connections (often Public Transport).
    /// A timetable offers connections which depart between 'startTime' and 'endTime' and provides the ID's of the previous and next timetable
    /// </summary>
    public interface ITimeTable
    {
        /// <summary>
        /// The moment when the earliest connections of this time table leave
        /// </summary>
        /// <returns></returns>
        DateTime StartTime();
        /// <summary>
        /// The moment when no more connections of this timetable leave.
        /// Thus: if e.g. 10:42 is given, the latest departing connections will probably depart at 10:41
        /// </summary>
        /// <returns></returns>
        DateTime EndTime();
        DateTime PreviousTableTime();
        DateTime NextTableTime();
        Uri NextTable();
        Uri PreviousTable();
        Uri Id();

        /// <summary>
        /// Get all the connections, earliest departure first
        /// </summary>
        /// <returns></returns>
        IEnumerable<IConnection> Connections();
        /// <summary>
        /// Get all the connections, latest departure time first
        /// </summary>
        /// <returns></returns>
        IEnumerable<IConnection> ConnectionsReversed();

        string ToString(ILocationProvider locationDecoder);
        string ToString(ILocationProvider locationDecoder, List<Uri> stopsWhitelist);

    }
}