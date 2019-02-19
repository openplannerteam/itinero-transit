using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.LC.IO.LC.Synchronization
{
    /// <summary>
    /// This class keeps track of one or more 'SynchronizerPolicies' and triggers them when needed
    /// </summary>
    public class Synchronizer
    {
        private readonly List<SynchronizedWindow> _policies;
        private readonly uint _clockRate;
        private readonly TransitDb _db;
        private bool _firstRun = true;

        public Synchronizer(TransitDb db, List<SynchronizedWindow> policies)
        {
            _db = db;
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
            var timer = new Timer(clockRate);
            timer.Elapsed += RunAll;
            timer.Start();
        }

        public Synchronizer(TransitDb db, params SynchronizedWindow[] policies) :
            this(db, new List<SynchronizedWindow>(policies))
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
        }


        public void RunAll(Object sender = null, ElapsedEventArgs eventArgs = null)
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
                    policy.Run(triggerDate, _db);
                }
                catch (Exception e)
                {
                    Log.Error("Running synchronization failed:\n" + e);
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