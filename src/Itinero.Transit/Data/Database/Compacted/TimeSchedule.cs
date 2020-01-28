using System.Collections.Generic;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Compacted
{
    public class TimeSchedule : KeyList<uint>
    {
        /// <summary>
        /// The timings is a list of durations in seconds.
        ///
        /// For correct interpretation, it needs a departure time;
        ///
        /// For example, if a train has the following schedule:
        ///
        /// | Station | Time  | Event
        /// |---------|-------|-------
        /// | A       | 10:10 | Departure
        /// | B       | 10:45 | Arrival/begin of (un)boarding
        /// | B       | 10:48 | Departure from B
        /// | C       | 11:15 | Arrival at terminus: start of unboarding
        ///
        /// This will be encoded as.
        /// [0, 10*60, 45*60, 48*60, 75*60]
        ///
        /// In other words, there are two entries for each location,
        /// every time indicating 'arrival/start of boarding' and 'departure/end of boarding'. (or rather: start of driving -> end of driving)
        ///
        /// Only the first and terminus is do have a single entry, respectively giving first departure and last arrival times
        /// </summary>
        public TimeSchedule(IEnumerable<uint> timings) : base(timings)
        {
        }

        /// <summary>
        /// Returns when the last trip arrives
        /// </summary>
        public uint Latest()
        {
            return this[Count - 1];
        }
    }
}