using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// The connections-provider is an object responsible for giving all kinds of connections.
    /// It is able to provide connections for
    /// - Public transport connections from different providers
    /// - Internal transfers
    /// - Multimodal transfers (with walking/cycling)
    ///
    /// The connection provider is trip-aspecific and can be reused.
    /// Although the algorithms can be run with a few general subproviders (say: SNCB, De Lijn + Walking),
    /// ConnectionsProviders can be highly specific (e.g. foldable bike, private shuttle services, ...)
    ///
    /// </summary>
    public interface IConnectionsProvider
    {
        ITimeTable GetTimeTable(Uri id);

        /// <summary>
        /// Give a timetable which contains connections starting at includedTime.
        /// The timetable might include connections departing earlier or later
        /// </summary>
        /// <param name="includedTime">The departure time of connections that are needed</param>
        /// <returns>A timetable</returns>
        Uri TimeTableIdFor(DateTime includedTime);

    
     

    }
}