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

        Uri NextTable();
        Uri PreviousTable();
        Uri Id();

        /// <summary>
        /// Get all the connections, earliest departure first
        /// </summary>
        /// <returns></returns>
        List<IConnection> Connections();
    }
}