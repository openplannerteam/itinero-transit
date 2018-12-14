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
        /// <param name="time"></param>
        public static void MoveToPrevious(this ConnectionsDb.DepartureEnumerator enumerator, ulong time)
        {
            enumerator.MovePrevious();
            while (enumerator.DepartureTime > time)
            {
                if (!enumerator.MovePrevious())
                {
                    throw new ArgumentOutOfRangeException(
                        "EnumeratorException: departure time not found. Either to little connections are loaded in the database, or the query is to far in the future or in the past");
                }
            }
        }


        public static void MoveToNext(this ConnectionsDb.DepartureEnumerator enumerator, ulong time)
        {
            while (enumerator.DepartureTime < time)
            {
                if (!enumerator.MoveNext())
                {
                    throw new ArgumentOutOfRangeException(
                        "EnumeratorException: departure time not found. Either to little connections are loaded in the database, or the query is to far in the future or in the past");
                }
            }
        }
    }
}