using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.LC.IO.LC.Synchronization
{
    /// <summary>
    /// This class keeps track of the 'SynchronizerPolicies' which are in use and triggers them every now and then to load them
    /// </summary>
    public class Synchronizer
    {
        private readonly List<SynchronizationPolicy> _policies;
        private readonly uint _clockRate;
        private readonly TransitDbUpdater _db;
        private bool _firstRun = true;
        private Timer _timer;

        public SynchronizationPolicy CurrentlyRunning { get; private set; }
        public IReadOnlyList<(DateTime start, DateTime end)> LoadedTimeWindows => _db.LoadedTimeWindows;


        public Synchronizer(TransitDb db,
            Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateDb,
            IReadOnlyCollection<SynchronizationPolicy> policies)
        {
            _db = new TransitDbUpdater(db, updateDb);
            // Highest frequency should be run often and thus has priority
            _policies = policies.OrderBy(p => p.Frequency).ToList();
            if (policies.Count == 0)
            {
                throw new ArgumentException("At least one synchronization policy should be given");
            }

            var clockRate = _policies[0].Frequency;
            foreach (var policy in policies)
            {
                if (policy.Frequency <= 0)
                {
                    throw new ArgumentException("This policy has a frequency of zero");
                }

                clockRate = Gcd(clockRate, policy.Frequency);
            }

            _clockRate = clockRate;
            _timer = new Timer(clockRate * 1000);
            _timer.Elapsed += RunAll;
            _timer.Start();
        }

        public Synchronizer(TransitDb db, Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateDb,
            params SynchronizationPolicy[] policies) :
            this(db, updateDb, new List<SynchronizationPolicy>(policies))
        {
        }

        /// <summary>
        /// This method triggers all the update policies.
        /// This can be used for an initial prefetch
        /// </summary>
        public void InitialRun()
        {
            foreach (var policy in _policies)
            {
                var unixNow = DateTime.Now.ToUnixTime();
                var date = unixNow - unixNow % policy.Frequency;
                var triggerDate = date.FromUnixTime();
                policy.Run(triggerDate, _db);
            }

            _firstRun = false;
        }


        private void RunAll(Object sender = null, ElapsedEventArgs eventArgs = null)
        {
            var unixNow = DateTime.Now.ToUnixTime();
            var date = unixNow - unixNow % _clockRate;
            var triggerDate = date.FromUnixTime();
            foreach (var policy in _policies)
            {
                if (date % policy.Frequency != 0 && !_firstRun)
                {
                    // This one does not have to be triggered this cycle
                    continue;
                }

                try
                {
                    CurrentlyRunning = policy;
                    policy.Run(triggerDate, _db);
                }
                catch (Exception e)
                {
                    Log.Error("Running synchronization failed:\n" + e);
                }
                finally
                {
                    CurrentlyRunning = null;
                }
            }

            _firstRun = false;
        }


        private static uint Gcd(uint a, uint b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0 ? b : a;
        }
    }
}