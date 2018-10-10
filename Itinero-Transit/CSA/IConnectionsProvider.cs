using System;
using System.Collections.Generic;

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
        IConnection GetConnection(Uri id);
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
        /// A provider encodes connections with a URI representing a certain location (e.g. train station Gent).
        /// However, sometimes we will have to transfer between 'Train Station Gent' and 'Busplatform 1 in front of the station of Gent'.
        /// Every provider offers a method to give the coordinates (lat, lon) which corresponds to a location-URI that the provider gave earlier.
        /// These Tuples are in turn use e.g. for walking transfers.
        /// </summary>
        /// <param name="locationid"></param>
        /// <returns></returns>
       // TODO Tuple<float, float> GetLocationFpr(Uri location);
    }
}