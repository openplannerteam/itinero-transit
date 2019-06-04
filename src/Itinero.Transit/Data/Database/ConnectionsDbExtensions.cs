using System;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data
{
    internal static class ConnectionsDbExtensions
    {
        /// <summary>
        /// Moves the enumerator backwards in time until the specified time is reached
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="time">The time to move to.</param>
        public static void MovePrevious(this IConnectionEnumerator enumerator, ulong time)
        {
            if (!enumerator.MovePrevious(time.FromUnixTime()))
            {
                throw new Exception(
                    "EnumeratorException: departure time not found. Either too little connections are loaded in the database, or the query is to far in the future or in the past");
            }
        }

        /// <summary>
        /// Moves the enumerator forward in time until the specified time is reached
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="time">The time to move to.</param>
        public static void MoveNext(this IConnectionEnumerator enumerator, ulong time)
        {
            if (!enumerator.MoveNext(time.FromUnixTime()))
            {
                throw new Exception(
                    "EnumeratorException: departure time not found. Either too little connections are loaded in the database, or the query is to far in the future or in the past");
            }
        }
    }
}