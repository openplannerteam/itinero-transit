using System.Collections.Generic;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface IConnectionsDb : IDatabaseReader<ConnectionId, Connection>, IClone<IConnectionsDb>
    {
        /// <summary>
        /// The earliest date loaded into this DB
        /// </summary>
        ulong EarliestDate { get; }

        /// <summary>
        /// The latest date loaded into this DB
        /// </summary>
        ulong LatestDate { get; }

        IConnectionEnumerator GetEnumeratorAt(ulong departureTime);

        void PostProcess();
    }


    /// <summary>
    /// THe connectionEnumerator offers
    /// - MoveNext
    /// - MovePrevious
    /// - CurrentTime
    ///
    /// As they can be used in combination, following properties are respected:
    ///
    /// When constructing the enumerator, the 'Current' value is undefined until MoveNext/MovePrevious is called
    ///
    /// When the enumerator is depleted, CurrentTime will be either 'ulong.MaxValue' or ulong.MinValue, when depleted by
    /// resp. moveNext/movePrevious
    /// 
    /// </summary>
    public interface IConnectionEnumerator : IEnumerator<ConnectionId>
    {
        bool MovePrevious();

        /// <summary>
        /// The departure time of the currently loaded connection.
        /// Gives ulong.MaxValue if the enumerator is depleted by MoveNext
        /// Gives ulong.MinValue (aka zero) if the enumerator is depleted by MovePrevious
        /// </summary>
        ulong CurrentTime { get; }
    }

    public static class ConnectionEnumeratorExtensions
    {
        public static uint Count(this IConnectionEnumerator enumerator)
        {
            var count = 0u;
            while (enumerator.MoveNext())
            {
                count++;
            }

            return count;
        }
    }
}