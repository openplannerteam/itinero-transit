using System.Collections.Generic;

namespace Itinero.Transit.Data
{
    public interface IStopsReader : IStop
    {
        bool MoveTo(LocationId stop);
        bool MoveTo(string globalId);
        void Reset();

        List<IStopsReader> UnderlyingDatabases { get; }

        /// <summary>
        /// Gives the internal StopsDb.
        /// Escapes the abstraction, should only be used for internal operations
        /// </summary>
        /// <returns></returns>
        StopsDb StopsDb { get; }
    }

    public static class StopsReaderExtensions
    {
        public static List<IStopsReader> FlattenedUnderlyingDatabases(this IStopsReader stopsReader)
        {

            if (stopsReader.UnderlyingDatabases == null)
            {
                return new List<IStopsReader>{stopsReader};
            }
            
            
            var list = new List<IStopsReader>();
            list.AddUnderlyingFlattened(stopsReader);
            return list;
        }
       
        private static void AddUnderlyingFlattened(this List<IStopsReader> flattened, IStopsReader stopsReader)
        {
            
            foreach (var underlying in stopsReader.UnderlyingDatabases)
            {
                if (underlying.UnderlyingDatabases == null)
                {
                    flattened.Add(underlying);
                }
                else
                {
                    flattened.AddUnderlyingFlattened(underlying);
                }
            }
        }
    }
}