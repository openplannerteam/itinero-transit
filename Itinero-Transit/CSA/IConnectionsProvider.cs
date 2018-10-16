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

        /// <summary>
        /// Given two connections (e.g. within the same station; or to a bus station which is close by),
        /// calculates an object representing the transfer (e.g. walking from platform 2 to platform 5; or walking 250 meters)
        /// </summary>
        /// <param name="from">The connection that the newly calculated connection continues on</param>
        /// <param name="to">The connection that should be taken after the returned connection</param>
        /// <returns>A connection representing the transfer. Returns null if no transfer is possible (e.g. to little time)</returns>
        IConnection CalculateInterConnection(IConnection from, IConnection to);

        /// <summary>
        /// A Connection Provider must also provide metadata about each transport stop and where this transport stop is located in the world.
        /// This is delegated to a LocationProvider object
        /// </summary>
        /// <returns></returns>
        ILocationProvider LocationProvider();


    }
}