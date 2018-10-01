using System;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.Data;
using Serilog;

namespace Itinero_Transit.LinkedData
{
    /// <summary>
    /// A wrapper around any other provider. Will serialize every requested object into the storage (passed during construction).
    /// If a request is already in the store, the stored version will be returned and the network won't be used.
    /// (Note that this might disable realtime information.
    ///
    /// The 'caching policy' of this provider is simple: retain forever - although the caller can clear the storage.
    ///
    ///NOte that 'calculateInterConnection' is _not_ cached
    /// 
    /// </summary>
    public class LocallyCachedConnectionsProvider : IConnectionsProvider
    {
        private readonly IConnectionsProvider _fallbackProvider;
        private readonly LocalStorage _storage;

        public LocallyCachedConnectionsProvider(IConnectionsProvider fallbackProvider, LocalStorage storage)
        {
            _fallbackProvider = fallbackProvider;
            _storage = storage;
        }

        public IConnection GetConnection(Uri id)
        {
            return _storage.Contains(id.OriginalString)
                ? _storage.Retrieve<IConnection>(id.OriginalString)
                : _storage.Store(id.OriginalString,
                    _fallbackProvider.GetConnection(id));
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            if (_storage.Contains(id.OriginalString))
            {
                return _storage.Retrieve<SncbTimeTable>(id.OriginalString);
            }

            var tt = _fallbackProvider.GetTimeTable(id);
            _storage.Store(tt.Id().OriginalString, tt);
            return tt;
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            var foundTt = TimeTableContaining(includedTime);
            return foundTt != null ? foundTt.Id() : _fallbackProvider.TimeTableIdFor(includedTime);
        }

        public IConnection CalculateInterConnection(IConnection from, IConnection to)
        {
            return _fallbackProvider.CalculateInterConnection(from, to);
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
            var wanted = _fallbackProvider.TimeTableIdFor(date).OriginalString;

            var keys = _storage.KnownKeys();

            var index = keys.BinarySearch(wanted);
            if (index >= 0)
            {
                return GetTimeTable(new Uri(keys[index]));
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