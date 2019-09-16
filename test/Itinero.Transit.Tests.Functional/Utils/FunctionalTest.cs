using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;

// ReSharper disable UnusedMember.Global
namespace Itinero.Transit.Tests.Functional.Utils
{
    /// <summary>
    /// Abstract definition of a functional test.
    /// </summary>
    public abstract class FunctionalTest
    {
        
        /// <summary>
        /// Executes this test for the given input.
        /// </summary>
        protected abstract void Execute();


        public void Run()
        {
            var start = DateTime.Now;
            Execute();
            var end = DateTime.Now;
            Information($"[OK] {Name} took {(end - start).TotalMilliseconds}ms");
        }
        
        public string Name => GetType().Name;
        public string LogPrefix = "";
        
        /// <summary>
        /// Asserts that the given value is true.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        protected void True(bool value)
        {
            if (!value)
            {
                throw new Exception("Assertion failed, expected true");
            }
        }

        protected void True(bool value, string msg)
        {
            if (!value)
            {
                throw new Exception("Assertion failed: " + msg);
            }
        }

        public void NotNull(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }
        }

        public void NotNull(object o, string message)
        {
            if (o == null)
            {
                throw new ArgumentException("Null detected: " + message);
            }
        }


        public void AssertContains(object o, IEnumerable xs)
        {
            foreach (var x in xs)
            {
                if (x.Equals(o))
                {
                    return;
                }
            }

            throw new Exception($"Element {o} was not found");
        }

        public static void AssertNoLoops<T>(Journey<T> journey, WithTime<TransferMetric> info)
            where T : IJourneyMetric<T>
        {
            AssertNoLoops(journey, info.StopsReader, info.ConnectionReader);
        }

        private static bool ContainsLoop<T>(Journey<T> journey)
            where T : IJourneyMetric<T>
        {
            var seen = new HashSet<StopId>();
            var curStop = journey.Location;

            while (journey != null)
            {
                if (!curStop.Equals(journey.Location))
                {
                    if (seen.Contains(journey.Location))
                    {
                        return true;
                    }

                    seen.Add(curStop);
                    curStop = journey.Location;
                }

                journey = journey.PreviousLink;
            }

            return false;
        }

        public static void AssertNoLoops<T>(Journey<T> journey, IStopsReader stops,
            IDatabaseReader<ConnectionId, Connection> conn) where T : IJourneyMetric<T>
        {
            if (ContainsLoop(journey))
            {
                throw new Exception("Loop detected in the journey: " + journey.ToString(50, stops, conn));
            }
        }
        
        public void AssertAreSame(ICollection<Journey<TransferMetric>> js, ICollection<Journey<TransferMetric>> bs,
            IStopsReader reader)
        {
            bool oneMissing = false;
            foreach (var a in js)
            {
                if (!bs.Contains(a))
                {
                    Logging.Log.Error($"Missing journey: {a.ToString(100, reader)}");
                    oneMissing = true;
                }
            }

            int bi = 0;
            foreach (var b in bs)
            {
                if (!js.Contains(b))
                {
                    Logging.Log.Error($"Missing journey {bi}: {b.ToString(100, reader)}");
                    oneMissing = true;
                }

                bi++;
            }

            True(!oneMissing);
        }

        /// <summary>
        /// Write a log event with the Informational level.
        /// </summary>
        /// <param name="message">The log message.</param>
        protected void Information(string message)
        {
            Serilog.Log.Information(LogPrefix+message);
        }
    }
}