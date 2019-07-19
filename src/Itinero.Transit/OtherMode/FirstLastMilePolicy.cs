using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;

namespace Itinero.Transit.OtherMode
{
    /// <summary>
    /// Applies a different walk policy depending on first mile/last mile
    /// </summary>
    public class FirstLastMilePolicy : IOtherModeGenerator
    {
        private readonly IOtherModeGenerator _defaultWalk;
        private readonly IOtherModeGenerator _firstMile;
        private readonly IOtherModeGenerator _lastMile;
        private readonly uint _range;
        private readonly HashSet<StopId> _firstMileStops;
        private readonly HashSet<StopId> _lastMileStops;

        public FirstLastMilePolicy(
            IOtherModeGenerator otherModeGeneratorImplementation,
            IOtherModeGenerator firstMile, IEnumerable<StopId> firstMileStops,
            IOtherModeGenerator lastMile, IEnumerable<StopId> lastMileStops)
        {
            _firstMile = firstMile;
            _firstMileStops = new HashSet<StopId>(firstMileStops);
            _lastMile = lastMile;
            _lastMileStops = new HashSet<StopId>(lastMileStops);
            _defaultWalk = otherModeGeneratorImplementation;
            _range = Math.Max(firstMile.Range(),
                Math.Max(lastMile.Range(), _defaultWalk.Range()));
        }

        public FirstLastMilePolicy(
            IOtherModeGenerator otherModeGeneratorImplementation,
            IOtherModeGenerator firstMile, IStop firstMileStop,
            IOtherModeGenerator lastMile, IStop lastMileStop) : this(
            otherModeGeneratorImplementation, firstMile, new[]
            {
                firstMileStop.Id
            }, lastMile, new[] {lastMileStop.Id})
        {
        }

        public uint TimeBetween(IStop from, IStop to)
        {
            return SelectSource(from.Id, to.Id).TimeBetween(from, to);
        }

        public Dictionary<StopId, uint> TimesBetween(IStop @from,
            IEnumerable<IStop> to)
        {
            if (_firstMileStops.Contains(from.Id))
            {
                // The from is a firstmile stop -> Everything should be handled with the first-mile logic
                return _firstMile.TimesBetween(from, to);
            }

            // Partition the 'stops'
            var tosDefault = new List<IStop>();
            var tosLastMile = new List<IStop>();

            foreach (var stop in to)
            {
                if (_lastMileStops.Contains(stop.Id))
                {
                    tosLastMile.Add(stop);
                }
                else
                {
                    tosDefault.Add(stop);
                }
            }

            if (tosLastMile.Count > 0 && tosDefault.Count > 0)
            {
                var a = _lastMile.TimesBetween(from, tosLastMile);
                var b = _defaultWalk.TimesBetween(from, tosDefault);

                foreach (var kv in a)
                {
                    try
                    {
                        b.Add(kv.Key, kv.Value);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Could not merge dictionaries: {e}. {kv.Key} --> {kv.Value}, old value {b[kv.Key]}");
                    }
                }

                return b;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (tosLastMile.Count > 0)
            {
                // No default walks
                return _lastMile.TimesBetween(from, tosLastMile);
            }

            return _defaultWalk.TimesBetween(@from, tosDefault);
        }

        public Dictionary<StopId, uint> TimesBetween(IEnumerable<IStop> @from,
            IStop to)
        {
            var firstMiles = new List<IStop>();
            var defaults = new List<IStop>();

            foreach (var stop in from)
            {
                if (_firstMileStops.Contains(stop.Id))
                {
                    firstMiles.Add(stop);
                }
                else
                {
                    defaults.Add(stop);
                }
            }

            var firstMileWalks = _firstMile.TimesBetween(firstMiles, to);

            Dictionary<StopId, uint> defaultWalks;
            if (_lastMileStops.Contains(to.Id))
            {
                defaultWalks = _lastMile.TimesBetween(defaults, to);
            }
            else
            {
                defaultWalks = _defaultWalk.TimesBetween(defaults, to);
            }


            if (firstMileWalks == null || firstMileWalks.Count == 0)
            {
                return defaultWalks;
            }

            if (defaultWalks == null)
            {
                return firstMileWalks;
            }

            foreach (var walk in firstMileWalks)
            {
                defaultWalks[walk.Key] = walk.Value;
            }

            return defaultWalks;
        }

        public uint Range()
        {
            return _range;
        }

        /// <summary>
        /// Selects the immediately underlying IOtherModeGenerator, which is needed for calculations
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IOtherModeGenerator SelectSource(StopId from, StopId to)
        {
            if (_firstMileStops.Contains(from))
            {
                return _firstMile;
            }

            if (_lastMileStops.Contains(to))
            {
                return _lastMile;
            }
            return _defaultWalk;
        }

        /// <summary>
        /// Gets the OtherModeGenerator which -in the end- is used to generate the actual route.
        /// Useful reconstructing the actual route in a frontend.
        /// Equivalent to 'SelectSource(from, to).GetSource(from, to)'
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IOtherModeGenerator GetSource(StopId from, StopId to)
        {
            return SelectSource(from, to).GetSource(from, to);
        }

        public string OtherModeIdentifier()
        {
            return
                $"firstLastMile" +
                $"&default={Uri.EscapeUriString(_defaultWalk.OtherModeIdentifier())}" +
                $"&firstMile={Uri.EscapeUriString(_firstMile.OtherModeIdentifier())}" +
                $"&lastMile={Uri.EscapeUriString(_lastMile.OtherModeIdentifier())}"
                ;
        }
    }
}