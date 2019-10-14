using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Synchronization
{
    /// <summary>
    /// This class keeps track of the 'SynchronizerPolicies' which are in use and triggers them every now and then to load them
    /// </summary>
    public class Synchronizer
    {
        private readonly List<ISynchronizationPolicy> _policies;
        private readonly uint _clockRate;
        private readonly TransitDbUpdater _db;
        private bool _firstRun = true;
        private readonly Timer _timer;

        public ISynchronizationPolicy CurrentlyRunning { get; private set; }

        // ReSharper disable once UnusedMember.Global
        public IReadOnlyList<(DateTime start, DateTime end)> LoadedTimeWindows => _db.LoadedTimeWindows;


        public Synchronizer(TransitDb db,
            Action<TransitDbWriter, DateTime, DateTime> updateDb,
            IReadOnlyCollection<ISynchronizationPolicy> policies,
            uint initialDelaySeconds = 1)
        {
            _db = new TransitDbUpdater(db, updateDb);
            // Highest frequency should be run often and thus has priority
            // Note that Frequency is actually (and confusingly) the delay between two runs, so lower = more often
            _policies = policies.OrderBy(p => p.Frequency).ToList();
            if (policies.Count == 0)
            {
                _policies.Add(new SynchronizedWindow(60, TimeSpan.Zero, TimeSpan.FromHours(3)));
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
            _timer = new Timer(Math.Min(initialDelaySeconds, clockRate) *
                               1000); // Clockrate is in seconds, timer expects millis
            _timer.Elapsed += RunAll;
        }

        public void Start()
        {
            _timer.Start();

            var txt = "";
            foreach (var policy in _policies)
            {
                txt += $"    Freq: 1/{policy.Frequency}sec: {policy}\n";
            }

            Log.Verbose(
                $"Started an automated task timer with clockrate {_clockRate} sec. Included policies are:\n{txt}");
        }

        public Synchronizer(TransitDb db, Action<TransitDbWriter, DateTime, DateTime> updateDb,
            uint initialDelaySeconds,
            params ISynchronizationPolicy[] policies) :
            this(db, updateDb, new List<ISynchronizationPolicy>(policies), initialDelaySeconds)
        {
        }

        public Synchronizer(TransitDb db, Action<TransitDbWriter, DateTime, DateTime> updateDb,
            params ISynchronizationPolicy[] policies) :
            this(db, updateDb, new List<ISynchronizationPolicy>(policies))
        {
        }

        /// <summary>
        /// This method triggers all the update policies.
        /// This can be used for an initial prefetch
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void InitialRun()
        {
            foreach (var policy in _policies)
            {
                var unixNow = DateTime.Now.ToUniversalTime().ToUnixTime();
                var date = unixNow - unixNow % policy.Frequency;
                var triggerDate = date.FromUnixTime();

                CurrentlyRunning = policy;
                Log.Verbose($"Currently running automated task (via initialRun) :{policy}");
                policy.Run(triggerDate, _db);
                Log.Verbose($"Done running automated task (via initialRun) :{policy}");
            }

            CurrentlyRunning = null;
            _firstRun = false;
        }


        private void RunAll(Object sender = null, ElapsedEventArgs eventArgs = null)
        {
            _timer.Interval = _clockRate * 1000;


            var unixNow = DateTime.Now.ToUniversalTime().ToUnixTime();
            var date = unixNow - unixNow % _clockRate;
            var triggerDate = date.FromUnixTime();

            if (CurrentlyRunning != null)
            {
                Log.Verbose("Tasks are already running... Skipping automated tasks for this tick");
                return;
            }

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
                    Log.Verbose($"Currently running automated task:{policy}");
                    policy.Run(triggerDate, _db);
                    Log.Verbose($"Done running automated task:{policy}");
                }
                catch (Exception e)
                {
                    Log.Error($"Running automated task {policy} failed:\n" + e);
                }

                CurrentlyRunning = null;
            }

            CurrentlyRunning = null;
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

        public void Stop()
        {
            _timer.Stop();
        }
    }
}