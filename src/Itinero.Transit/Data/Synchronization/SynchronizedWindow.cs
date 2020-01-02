using System;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Data.Synchronization
{
    /// <summary>
    /// A 'synchronizedWindow' represents a time window that should be (re)loaded regularly.
    ///
    /// It should be triggered every `freq`-seconds and will then, based on the time it was triggered, calculate what window should be loaded by the transitDB.
    /// 
    /// </summary>
    public class SynchronizedWindow : ISynchronizationPolicy
    {
        public uint Frequency { get; }
        private TimeSpan LoadBefore { get; }
        private TimeSpan LoadAfter { get; }
        private readonly uint _retries;
        private readonly bool _forceUpdate;

        // State leak to provide update reports. 
        private DateTime? _triggeredDate;

        /// <summary>
        /// Create a new synchronization  policy
        /// </summary>
        /// <param name="frequency">How often this policy should be triggered, in seconds</param>
        /// <param name="loadBefore">The (minimum) timespan before the trigger date that should be loaded into the transitDB (it might overshoot a little)</param>
        /// <param name="loadAfter">The (minimum) timespan that should be loaded after the the trigger date (it might overshoot a little)</param>
        /// <param name="retries">When the update fails, indicates how many times it will be retried. Default: 0</param>
        /// <param name="forceUpdate">If set, it will _update_ the connections already present in the transitDB. The default behaviour is not to download already present time windows</param>
        public SynchronizedWindow(
            uint frequency,
            TimeSpan loadBefore,
            TimeSpan loadAfter, uint retries = 0, bool forceUpdate = false
        )
        {
            Frequency = frequency;

            LoadBefore = loadBefore;

            LoadAfter = loadAfter;
            _retries = retries;
            _forceUpdate = forceUpdate;
        }


        public void Run(DateTime triggeredOn, TransitDbUpdater toUpdate)
        {
            _triggeredDate = triggeredOn;
            var start = triggeredOn - LoadBefore;
            var end = triggeredOn + LoadAfter;
            var attempts = 0;
            while (attempts <= _retries)
            {
                attempts++;
                try
                {
                    toUpdate.UpdateTimeFrame(start, end, _forceUpdate);
                }
                catch (Exception e)
                {
                    Log.Warning(
                        $"Updating timewindow {start} --> {end} failed: {e.Message}\nThis is attempt {attempts} out of {_retries}");
                }
            }

            _triggeredDate = null;
        }


        public override string ToString()
        {
            var updates = _forceUpdate ? "forces updating" : "no overwrite";
            var retr = _retries == 0
                ? "No retries on failing"
                : _retries == 1 ? "Single retry on failing" : $"{_retries} retries when failing";
            var now = _triggeredDate == null ? "" : $" Now running with trigger date {_triggeredDate.Value:O}";
            return
                $"SynchronizedWindow, {LoadBefore:g} --> {LoadAfter:g} (triggers every {TimeSpan.FromSeconds(Frequency)}, {updates}, {retr}){now}";
        }
    }
}