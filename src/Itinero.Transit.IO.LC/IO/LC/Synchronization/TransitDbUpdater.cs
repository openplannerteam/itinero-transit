using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;

namespace Itinero.Transit.IO.LC.IO.LC.Synchronization
{
    
    /// <summary>
    /// A small helper class, which keeps track of
    ///
    /// - Which timewindows are already loaded
    /// - A callback to actually update the transitdb
    ///
    /// 
    /// </summary>
    public class TransitDbUpdater
    {
        private readonly TransitDb _tdb;
        private readonly Action<TransitDb.TransitDbWriter, DateTime, DateTime> _updateTimeFrame;
        private readonly DateTracker _loadedTimeWindows = new DateTracker();

        public List<(DateTime start, DateTime end)> LoadedTimeWindows => _loadedTimeWindows.TimeWindows();

        
        public TransitDbUpdater(TransitDb tdb, Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateTimeFrame)
        {
            _tdb = tdb;
            _updateTimeFrame = updateTimeFrame;
        }
        
        
        /// <summary>
        /// Loads more data into the transitDB, as specified by the callbackfunction.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="refresh">If true, overwrites. If false, only the gaps will be filled</param>
        public void UpdateTimeFrame(DateTime start, DateTime end, bool refresh = false)
        {
            if (_updateTimeFrame == null)
            {
                // Seems like we are running tests... SKIP!
                return;
            }

            var gaps = new List<(DateTime, DateTime)>();
            if (refresh)
            {
                gaps.Add((start, end));
            }
            else
            {
                gaps = _loadedTimeWindows.Gaps(start, end);
            }

            if (gaps.Count != 0)
            {
                // No work to do
                return;
            }

            var writer = _tdb.GetWriter();
            try
            {
                foreach (var (wStart, wEnd) in gaps)
                {
                    _updateTimeFrame.Invoke(writer, wStart, wEnd);
                    _loadedTimeWindows.AddTimeWindow(wStart, wEnd);
                }
            }
            finally
            {
                writer.Close();
            }
        }

    }
    
    
}