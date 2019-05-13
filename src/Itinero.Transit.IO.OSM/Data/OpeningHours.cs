using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data
{
    public class OpeningHours
    {
        public const string Open = "open";
        public const string Closed = "closed";

        public static IOpeningHoursRule Parse(string value, string timezone)
        {
            value = value.ToLower();
            var rule = ((IOpeningHoursRule) TwentyFourSeven.TryParse(value) ??
                        DaysOfWeekRule.TryParse(value)) ??
                       HoursRule.TryParse(value);
            return new TimeZoneRewriter(rule, timezone);
        }
    }


    public interface IOpeningHoursRule
    {
        /// <summary>
        /// 
        /// Returns the moment of next change. Must return a strictly later value
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        DateTime NextChange(DateTime from);

        DateTime PreviousChange(DateTime from);

        /// <summary>
        /// Returns the state at the given moment - if applicable.
        /// Return null if no state could be determined.
        /// </summary>
        /// <param name="moment"></param>
        /// <returns></returns>
        string StateAt(DateTime moment);
    }

    public static class OpeningHoursRule
    {
        public static string StateAt(this IOpeningHoursRule rule, DateTime moment, string fallback)
        {
            return rule.StateAt(moment) ?? fallback;
        }
    }

    public class TimeZoneRewriter : IOpeningHoursRule
    {
        private IOpeningHoursRule _openingHoursRuleImplementation;
        private readonly TimeZoneInfo _timezone;

        public TimeZoneRewriter(IOpeningHoursRule openingHoursRuleImplementation, string timezone)
        {
            _openingHoursRuleImplementation = openingHoursRuleImplementation;
            _timezone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }

        private DateTime ApplyTimeZone(DateTime dt)
        {
            return TimeZoneInfo.ConvertTime(dt, _timezone);
        }

        public DateTime NextChange(DateTime from)
        {
            return _openingHoursRuleImplementation.NextChange(ApplyTimeZone(from))
                .ToUniversalTime();
        }

        public DateTime PreviousChange(DateTime from)
        {
            return _openingHoursRuleImplementation.PreviousChange(ApplyTimeZone(from))
                .ToUniversalTime();
        }

        public string StateAt(DateTime moment)
        {
            return _openingHoursRuleImplementation.StateAt(ApplyTimeZone(moment));
        }
    }


    public class DaysOfWeekRule : IOpeningHoursRule
    {
        private static readonly List<string> _weekdays =
            new List<string> {"su", "mo", "tu", "we", "th", "fr", "sa"};

        private bool[] _weekdayMask;
        private IOpeningHoursRule _containedRule;

        public DaysOfWeekRule(bool[] weekdayMask, IOpeningHoursRule containedRule)
        {
            _weekdayMask = weekdayMask;
            _containedRule = containedRule;
        }

        private static bool[] ParseWeekdays(string value)
        {
            var weekdayFlags = new bool[7];
            for (var i = 0; i < weekdayFlags.Length; i++)
            {
                weekdayFlags[i] = false;
            }

            foreach (var weekdayRange in value.Split(','))
            {
                if (weekdayRange.Contains("-"))
                {
                    var split = weekdayRange.Split('-');
                    if (split.Length > 2)
                    {
                        throw new ArgumentException("To many dashes in weekday-window");
                    }

                    var start = _weekdays.IndexOf(split[0]);
                    var end = _weekdays.IndexOf(split[1]);
                    if (start < 0 || end < 0)
                    {
                        throw new ArgumentException("Unknown weekday: " + weekdayRange);
                    }

                    for (var i = start; (i % 7) != end; i++)
                    {
                        weekdayFlags[i] = true;
                    }

                    weekdayFlags[end] = true;
                }
                else
                {
                    var i = _weekdays.IndexOf(weekdayRange);

                    if (i < 0)
                    {
                        throw new ArgumentException("Unknown weekday: " + weekdayRange);
                    }

                    weekdayFlags[i] = true;
                }
            }

            return weekdayFlags;
        }

        private static IOpeningHoursRule ContainedRule(string[] splitted)
        {
            var state = OpeningHours.Open;
            // An eventual state can be found in the last part, if it can not be parsed
            var last = HoursRule.TryParse(splitted.Last());
            if (last == null)
            {
                state = splitted.Last();
            }


            var subrules = splitted.SubArray(1, splitted.Length - 2); // Minus 2 - the last one is special too
            var parsed = new List<IOpeningHoursRule>();
            foreach (var subrule in subrules)
            {
                parsed.Add(HoursRule.TryParse(subrule, state));
            }

            if (last != null)
            {
                parsed.Add(last);
            }

            return new PriorityRules(parsed);
        }

        public static DaysOfWeekRule TryParse(string value)
        {
            var splitted = value.Split(' ');
            var weekdayRange = ParseWeekdays(splitted[0]);
            var contained = ContainedRule(splitted);
            return new DaysOfWeekRule(weekdayRange, contained);
        }

        private bool InRange(DateTime moment)
        {
            return _weekdayMask[(int) moment.DayOfWeek];
        }

        public DateTime NextChange(DateTime @from)
        {
            while (!InRange(from))
            {
                from = from.Date.AddDays(1);
            }

            return _containedRule.NextChange(from);
        }

        public DateTime PreviousChange(DateTime @from)
        {
            while (!InRange(from))
            {
                from = from.Date.AddDays(-1);
            }

            return _containedRule.PreviousChange(from);
        }

        public string StateAt(DateTime moment)
        {
            if (InRange(moment))
            {
                return _containedRule.StateAt(moment);
            }

            return null;
        }
    }


    public class HoursRule : IOpeningHoursRule
    {
        private readonly TimeSpan _start;
        private readonly TimeSpan _stop;
        private readonly string _state;

        public HoursRule(TimeSpan start, TimeSpan stop, string state = OpeningHours.Open)
        {
            _start = start;
            _stop = stop;
            if (_stop < _start)
            {
                throw new ArgumentException(
                    "Closing after midnight is not supported. Make sure the start date falls before the end time");
            }

            _state = state;
        }

        public static HoursRule TryParse(string value, string state = OpeningHours.Open)
        {
            if (TimeSpan.TryParseExact(value, "HH\\:mm", null, out var moment))
            {
                return new HoursRule(moment, moment, state);
            }

            if (!value.Contains("-")) return null;


            var split = value.Split('-');
            if (split.Length > 2)
            {
                throw new ArgumentException("To many dashes in hour-window");
            }

            var start = split[0];
            var end = split[1];
            if (!TimeSpan.TryParseExact(start, "hh\\:mm", null, out var startMoment))
            {
                return null;
            }

            if (!TimeSpan.TryParseExact(end, "hh\\:mm", null, out var endMoment))
            {
                return null;
            }

            return new HoursRule(startMoment, endMoment, state);
        }

        public DateTime NextChange(DateTime from)
        {
            var tod = from.TimeOfDay;
            // which is earliest?

            if (tod < _start)
            {
                return from.Date + _start;
            }

            if (tod < _stop)
            {
                return from.Date + _stop;
            }

            return from.Date.AddDays(1) + _start;
        }

        public DateTime PreviousChange(DateTime from)
        {
            var tod = from.TimeOfDay;
            // which is latest?

            if (tod > _stop)
            {
                return from.Date + _stop;
            }

            if (tod > _start)
            {
                return from.Date + _start;
            }


            return from.Date.AddDays(-1) - _stop;
        }

        public string StateAt(DateTime moment)
        {
            return moment.TimeOfDay >= _start && moment.TimeOfDay < _stop ? _state : null;
        }
    }

    public class PriorityRules : IOpeningHoursRule
    {
        private readonly List<IOpeningHoursRule> _rules;

        public PriorityRules(List<IOpeningHoursRule> rules)
        {
            _rules = rules;
        }

        public DateTime NextChange(DateTime from)
        {
            var earliest = _rules[0].NextChange(from);
            for (var i = 1; i < _rules.Count; i++)
            {
                var change = _rules[0].NextChange(from);
                if (earliest > change)
                {
                    earliest = change;
                }
            }

            return earliest;
        }

        public DateTime PreviousChange(DateTime from)
        {
            var latest = _rules[0].NextChange(from);
            for (var i = 1; i < _rules.Count; i++)
            {
                var change = _rules[0].PreviousChange(from);
                if (latest < change)
                {
                    latest = change;
                }
            }

            return latest;
        }

        public string StateAt(DateTime moment)
        {
            foreach (var rule in _rules)
            {
                var state = rule.StateAt(moment);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }
    }

    public class TwentyFourSeven : IOpeningHoursRule
    {
        private readonly string _state;

        public TwentyFourSeven(string state = OpeningHours.Open)
        {
            _state = state;
        }

        public static TwentyFourSeven TryParse(string input)
        {
            if (input.Equals("24/7") || input.Equals("24/7 open"))
            {
                return new TwentyFourSeven();
            }

            if (input.Equals("24/7 closed") || input.Equals("24/7 off"))
            {
                return new TwentyFourSeven(OpeningHours.Closed);
            }

            return null;
        }

        public DateTime NextChange(DateTime from)
        {
            return DateTime.MaxValue;
        }

        public DateTime PreviousChange(DateTime from)
        {
            return DateTime.MinValue;
        }

        public string StateAt(DateTime moment)
        {
            return _state;
        }
    }
}