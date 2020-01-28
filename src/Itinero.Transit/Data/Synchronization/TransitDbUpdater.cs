using System;
using System.Collections.Generic;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Synchronization
{
    
    /// <summary>
    /// A small helper class, which keeps track of
    ///
    /// - Which timewindows are already loaded
    /// - A callback to actually update the transitdb
    /// 
    /// </summary>
    public class TransitDbUpdater
    {
        private readonly Action<IWriter, DateTime, DateTime> _updateTimeFrame;
        private readonly DateTracker _loadedTimeWindows = new DateTracker();

        public IReadOnlyList<(DateTime start, DateTime end)> LoadedTimeWindows => _loadedTimeWindows.TimeWindows();

        public TransitDb TransitDb { get; }

        public TransitDbUpdater(TransitDb tdb, Action<IWriter, DateTime, DateTime> updateTimeFrame)
        {
            TransitDb = tdb;
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
                gaps = _loadedTimeWindows.CalculateGaps(start, end);
            }

            if (gaps.Count == 0)
            {
                // No work to do
                return;
            }

            var writer = TransitDb.GetWriter();
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
                TransitDb.CloseWriter();
            }
        }
    }
    
    
}