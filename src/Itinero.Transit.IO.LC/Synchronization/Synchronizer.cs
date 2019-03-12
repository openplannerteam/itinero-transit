using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.LC.Synchronization
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
        public IReadOnlyList<(DateTime start, DateTime end)> LoadedTimeWindows => _db.LoadedTimeWindows;


        public Synchronizer(TransitDb db,
            Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateDb,
            IReadOnlyCollection<ISynchronizationPolicy> policies,
            uint initialDelaySeconds = 1)
        {
            _db = new TransitDbUpdater(db, updateDb);
            // Highest frequency should be run often and thus has priority
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
            _timer.Start();

            var txt = "";
            foreach (var policy in _policies)
            {
                txt += $"    Freq: 1/{policy.Frequency}sec: {policy}\n";
            }

            Log.Information(
                $"Started an automated task timer with clockrate {_clockRate} sec. Initial delay is {initialDelaySeconds} Included policies are:\n{txt}");
        }

        public Synchronizer(TransitDb db, Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateDb,
            uint initialDelaySeconds,
            params ISynchronizationPolicy[] policies) :
            this(db, updateDb, new List<ISynchronizationPolicy>(policies), initialDelaySeconds)
        {
        }

        public Synchronizer(TransitDb db, Action<TransitDb.TransitDbWriter, DateTime, DateTime> updateDb,
            params ISynchronizationPolicy[] policies) :
            this(db, updateDb, new List<ISynchronizationPolicy>(policies))
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

                CurrentlyRunning = policy;
                Log.Information($"Currently running automated task (via initialRun) :{policy}");
                policy.Run(triggerDate, _db);
                Log.Information($"Done running automated task (via initialRun) :{policy}");
            }

            _firstRun = false;
        }


        private void RunAll(Object sender = null, ElapsedEventArgs eventArgs = null)
        {
            _timer.Interval = _clockRate * 1000;

            
            var unixNow = DateTime.Now.ToUnixTime();
            var date = unixNow - unixNow % _clockRate;
            var triggerDate = date.FromUnixTime();

            if (CurrentlyRunning != null)
            {
                Log.Information("Tasks are already running... Skipping automated tasks for this tick");
                return;
            }

            try
            {
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
                        Log.Information($"Currently running automated task:{policy}");
                        policy.Run(triggerDate, _db);
                        Log.Information($"Done running automated task:{policy}");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Running automated task {policy} failed:\n" + e);
                    }
                }
            }
            finally
            {
                CurrentlyRunning = null;
                _firstRun = false;
            }
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