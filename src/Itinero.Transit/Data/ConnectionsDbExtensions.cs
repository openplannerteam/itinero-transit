using System;

namespace Itinero.Transit.Data
{
    public static class ConnectionsDbExtensions
    {
        /// <summary>
        /// Gets a reader() which is loaded on the connection.
        /// Use this for testing only, it is slow
        /// </summary>
        /// <returns></returns>
        public static IConnection LoadConnection(this ConnectionsDb db, uint id)
        {
            var reader = db.GetReader();
            reader.MoveTo(id);
            return reader;
        }


        /// <summary>
        /// Moves the enumerator backwards in time until the specified time is reached
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="time">The time to move to.</param>
        public static void MovePrevious(this ConnectionsDb.DepartureEnumerator enumerator, ulong time)
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
        public static void MoveNext(this ConnectionsDb.DepartureEnumerator enumerator, ulong time)
        {
            if (!enumerator.MoveNext(time.FromUnixTime()))
            {
                throw new Exception(
                    "EnumeratorException: departure time not found. Either too little connections are loaded in the database, or the query is to far in the future or in the past");
            }
        }
    }
}