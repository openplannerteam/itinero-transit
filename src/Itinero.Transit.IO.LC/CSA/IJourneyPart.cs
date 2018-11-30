using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    /// <summary>
    /// An IJourneyPart represents a single part in the journey.
    /// It has a departure and arrival time and location.
    ///
    /// Example IJOurneyParts are
    ///  - Connections (which are discrete)
    ///  - Walking parts (which are continuous)
    /// 
    /// </summary>
    public interface IJourneyPart
    {
        /// <summary>
        /// Where this connection starts
        /// </summary>
        /// <returns></returns>
        Uri DepartureLocation();

        /// <summary>
        /// Where this connection ends
        /// </summary>
        /// <returns></returns>
        Uri ArrivalLocation();

        /// <summary>
        /// When the connection starts
        /// </summary>
        /// <returns></returns>
        DateTime ArrivalTime();

        /// <summary>
        /// When the connection ends
        /// </summary>
        /// <returns></returns>
        DateTime DepartureTime();

        /// <summary>
        /// Gives this connection as an Itinero-route.
        /// Used mainly to convert into GeoJSON afterwards.
        /// </summary>
        /// <returns></returns>
        Route AsRoute(ILocationProvider locationProv);

        string ToString(ILocationProvider locationDecoder);

    }


    public class DepartureTimeConnectionComparer : IComparer<IConnection>
    {
        public static DepartureTimeConnectionComparer Singleton = new DepartureTimeConnectionComparer();

        public int Compare(IConnection x, IConnection y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentNullException();
            }

            return x.DepartureTime().CompareTo(y.DepartureTime());
        }
    }

    public class DepartureTimeConnectionComparerDesc : IComparer<IConnection>
    {
        public static DepartureTimeConnectionComparerDesc Singleton = new DepartureTimeConnectionComparerDesc();

        public int Compare(IConnection x, IConnection y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentNullException();
            }

            return y.DepartureTime().CompareTo(x.DepartureTime());
        }
    }
}