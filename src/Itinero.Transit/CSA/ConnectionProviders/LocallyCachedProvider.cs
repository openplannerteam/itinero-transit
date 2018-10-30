using System;
using Itinero.Transit.CSA;
using Itinero.Transit.CSA.ConnectionProviders;
using Itinero.Transit.CSA.Data;

namespace Itinero.Transit.LinkedData
{
    /// <inheritdoc />
    ///  <summary>
    ///  A wrapper around any other provider. Will serialize every requested object into the storage (passed during construction).
    ///  If a request is already in the store, the stored version will be returned and the network won't be used.
    ///  (Note that this might disable realtime information.
    ///  The 'caching policy' of this provider is simple: retain forever - although the caller can clear the storage.
    /// NOte that 'calculateInterConnection' is _not_ cached
    ///  </summary>
    public class LocallyCachedConnectionsProvider : IConnectionsProvider
    {
        private readonly IConnectionsProvider _fallbackProvider;
        private readonly LocalStorage _storage;

        public LocallyCachedConnectionsProvider(IConnectionsProvider fallbackProvider, LocalStorage storage)
        {
            _fallbackProvider = fallbackProvider;
            _storage = storage;
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            if (_storage.Contains(id.OriginalString))
            {
                return _storage.Retrieve<LinkedTimeTable>(id.OriginalString);
            }

            var tt = _fallbackProvider.GetTimeTable(id);
            _storage.Store(tt.Id().OriginalString, tt);
            return tt;
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            var foundTt = TimeTableContaining(includedTime)?.Id();
            return foundTt ?? _fallbackProvider.TimeTableIdFor(includedTime);
        }

        /// <summary>
        /// Fetches all timetables between the given times.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void PreFetch(DateTime start, DateTime end)
        {
            var tt = GetTimeTable(TimeTableIdFor(start));
            while (tt.EndTime() < end)
            {
                tt = GetTimeTable(tt.NextTable());
            }
        }

        /// <summary>
        /// Searches, within the local cache, the latest timetable just before the given moment in time
        /// </summary>
        /// <param name="date"></param>
        public ITimeTable TimeTableContaining(DateTime date)
        {
            var keys = _storage.KnownKeys();
            if (keys.Count == 0)
            {
                // The cache is empty
                return null;
            }
            
            var wanted = _fallbackProvider.TimeTableIdFor(date).OriginalString;

            var index = keys.BinarySearch(wanted);
            if (index >= 0)
            {
                return GetTimeTable(new Uri(keys[index]));
            }

            if (index == -1)
            {
                // Date falls before earliest cached moment
                return null;
            }

            if (~index - 1 >= keys.Count)
            {
                return null;
            }
            // We have found the time table in cache which might contain the requested time table
            // Lets instantiate it
            var tt = GetTimeTable(new Uri(keys[~index - 1])); // Always cached
            // One caveat: the found time table might be too early
            // We do an extra check and return null if the actually needed table is not there
            if (!(tt.StartTime() <= date && tt.EndTime() > date))
            {
                return null;
            }

            return tt;
        }
    }
}