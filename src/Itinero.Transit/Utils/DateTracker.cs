using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Itinero.Transit.Utils
{
    /// <summary>
    /// Basically a list of time windows.
    /// If two time windows overlap, they get merged into one window
    /// </summary>
    public class DateTracker
    {
        // Sorted by start-time. Contains _no_ overlaps
        private List<(DateTime start, DateTime end)> _allWindows = new List<(DateTime start, DateTime end)>();


        public void AddTimeWindow(DateTime start, DateTime end)
        {
            // First of all, search the point where this window should be inserted

            var insertionIndex = _allWindows.Count;
            var currentStart = DateTime.MaxValue;
            var currentEnd = DateTime.MinValue;
            for (int i = 0; i < _allWindows.Count; i++)
            {
                if (_allWindows[i].start >= start)
                {
                    insertionIndex = i;
                    (currentStart, currentEnd) = _allWindows[i];
                    break;
                }
            }

            if (start == currentStart && end == currentEnd)
            {
                // This exact timewindow is already included
                return;
            }

            // We now know at what position this time frame has to come
            // We selected the earliest start time that is later then the new start time

            // We might be able to reuse the previous entry, if there is overlap with the new window
            if (insertionIndex > 0 && start <= _allWindows[insertionIndex - 1].end)
            {
                // The new window overlaps with the previous window

                if (_allWindows[insertionIndex - 1].end > end)
                {
                    // The newly added window is completely eaten
                    return;
                }

                // We extend this already existing window
                _allWindows[insertionIndex - 1] = (_allWindows[insertionIndex - 1].start, end);

                // And check if we have to remove trailing entries
                CleanOverlap(insertionIndex, end);
                return;
            }


            if (end >= currentStart)
            {
                // This new window overlaps with the window currently at position 'insertionIndex'
                // We reuse this index

                var newStart = start; // The new start is de facto earlier
                var newEnd = currentEnd >= end ? currentEnd : end;
                _allWindows[insertionIndex] = (newStart, newEnd);

                // At last, the new window might 'eat' the following entries
                CleanOverlap(insertionIndex + 1, newEnd);
                return;
            }

            // This is a non-overlapping window
            // We simply insert it
            _allWindows.Insert(insertionIndex, (start, end));
            // The new window does not overlap with any other window
            // This means we are done
        }

        /// <summary>
        /// Removes all windows starting at index.
        /// If they are (partially) swallowed by the new window, the are removed
        /// </summary>
        /// <param name="index"></param>
        /// <param name="endTime"></param>
        private void CleanOverlap(int index, DateTime endTime)
        {
            while (index < _allWindows.Count && _allWindows[index].start <= endTime)
            {
                _allWindows[index - 1] = (_allWindows[index - 1].start, _allWindows[index].end);
                // Potentially O(n²). Not really that much of an issue, normally not that frequently used
                _allWindows.RemoveAt(index);
            }
        }


        [Pure]
        public List<(DateTime start, DateTime end)> TimeWindows()
        {
            return _allWindows;
        }

        /// <summary>
        /// Calculates the time windows of gaps, so that - if summed together - one continues timewindow would be the result
        /// In other words:
        /// (start -> end) minus every time window in this tracker 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [Pure]
        public List<(DateTime start, DateTime end)> CalculateGaps(DateTime start, DateTime end)
        {
            var result = new List<(DateTime start, DateTime end)>();


            if (!_allWindows.Any())
            {
                // No time windows loaded at all
                result.Add((start, end));
                return result;
            }

            // Select the smallest index so that 'start' is smaller then allWindows[index].start
            var index = _allWindows.Count - 1;
            for (var i = 0; i < _allWindows.Count; i++)
            {
                if (_allWindows[i].start >= start)
                {
                    index = i;
                    break;
                }
            }

            if (_allWindows[index].start <= start && _allWindows[index].end >= end)
            {
                // The window is completely covered
                // No gaps to find here!
                return result;
            }

            if (index == _allWindows.Count - 1 && _allWindows[index].end <= start)
            {
                // The window falls completely at the end
                result.Add((start, end));
                return result;
            }

            // Did we start in another window?
            if (index > 0 && _allWindows[index - 1].end > start)
            {
                // Yep, it seems so.
                // Not that big of an issue
                start = _allWindows[index - 1].end;
            }

            for (var i = index; i < _allWindows.Count; i++)
            {
                if (_allWindows[i].start > end)
                {
                    result.Add((start, end));
                    return result;
                }


                result.Add((start, _allWindows[i].start));
                start = _allWindows[i].end;
                if (start >= end)
                {
                    // We have reached the end of our window
                    return result;
                }
            }

            // At last: add the last fragment
            result.Add((start, end));


            return result;
        }
    }
}