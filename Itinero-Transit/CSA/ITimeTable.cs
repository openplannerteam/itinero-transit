using System;
using System.Collections.Generic;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A TimeTable is an object containing multiple connections (often Public Transport).
    /// A timetable offers connections which depart between 'startTime' and 'endTime' and provides the ID's of the previous and next timetable
    /// </summary>
    public interface ITimeTable
    {
        DateTime StartTime();
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