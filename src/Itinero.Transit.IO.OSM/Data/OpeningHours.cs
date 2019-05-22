using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Itinero.Transit.Data.OpeningHoursRDParser;
using Itinero.Transit.Logging;
using NodaTime;

namespace Itinero.Transit.Data
{
    public static class OpeningHours
    {
        /// <summary>
        /// Parse an opening hours rule.
        ///
        /// Supported:
        /// Month[-Month] Weekday[-Weekday] [hh:mm-hh:mm] state
        /// 
        /// NOT SUPPORTED ATM:
        /// PH (Abbreviation for public holiday)
        /// SH (Abbreviation for School holiday)
        /// Weekday[index] (indexing of weekdays, e.g. the first sunday of the month)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timezone"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        [Pure]
        public static IOpeningHoursRule ParseOpeningHoursRule(this string value, string timezone)
        {
            var parser =
                (OpeningHoursRdParsers.MultipleRules() |
                 OpeningHoursRdParsers.WeekdayRule() |
                 OpeningHoursRdParsers.HoursRule() |
                 OpeningHoursRdParsers.TwentyFourSeven() |
                 OpeningHoursRdParsers.OSMStatus()) + !OpeningHoursRdParsers.WS();


            var raw = parser.Parse(value);
            if (raw == null)
            {
                throw new FormatException("Could not parse " + value);
            }

            var (parsed, rest) = raw.Value;

            if (!string.IsNullOrEmpty(rest))
            {
                throw new FormatException(
                    $"Could not parse {value}, could not parse a part of the input. Rest is{rest}");
            }

            return new TimeZoneRewriter(parsed, timezone);
        }


        /// <summary>
        /// Attempts to parse:
        /// hh:mm:ss
        /// hh:mm
        /// mm
        /// and formats such as
        /// 15min, 15minutes, 15 min, 15 sec, 15h, 15u, ...
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TimeSpan TryParseTimespan(this string value)
        {
            value = value.ToLower().Replace(" ", "");
            var formats = new[]
            {
                "hh\\:mm\\:ss",
                "hh\\:mm",
                "mm",
                "hh\\h",
                "hh\\hour",
                "hh\\hours",
                "hh\\u",
                "mm\\m",
                "mm\\min",
                "mm\\minute",
                "mm\\minutes",
                "ss\\s",
                "ss\\sec",
                "ss\\second",
                "ss\\seconds",
            };
            foreach (var format in formats)
            {
                TimeSpan.TryParseExact(value, format, null, out var ts);
                if (ts != default(TimeSpan))
                {
                    return ts;
                }
            }

            throw new ArgumentException("Could not parse with any format" + value);
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
        [Pure]
        DateTime NextChange(DateTime from);

        [Pure]
        DateTime PreviousChange(DateTime from);

        /// <summary>
        /// Returns the state at the given moment - if applicable.
        /// Return null if no state could be determined.
        /// </summary>
        /// <param name="moment"></param>
        /// <returns></returns>
        [Pure]
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
        private readonly IOpeningHoursRule _openingHoursRuleImplementation;
        private readonly DateTimeZone _timezone;

        public TimeZoneRewriter(IOpeningHoursRule openingHoursRuleImplementation, string timezone)
        {
            _openingHoursRuleImplementation = openingHoursRuleImplementation;
            _timezone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezone);
            if (_timezone == null)
            {
                Log.Error("Could not find the timezone with NodaTime");
            }
        }

        private DateTime ApplyTimeZone(DateTime dt)
        {
            try
            {
                var instant = Instant.FromDateTimeUtc(dt.ToUniversalTime());
                var zonedInstant = instant.InZone(_timezone);
                return zonedInstant.ToDateTimeUnspecified();
            }
            catch
            {
                Log.Error("Nodatime did not find the timezone " + _timezone);
                return dt;
            }
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

        public override string ToString()
        {
            return _openingHoursRuleImplementation.ToString();
        }
    }


    public class DaysOfWeekRule : IOpeningHoursRule
    {
        private static readonly List<string> _weekdays =
            new List<string> {"mo", "tu", "we", "th", "fr", "sa", "su"};

        private readonly bool[] _weekdayMask;
        private readonly IOpeningHoursRule _containedRule;

        public DaysOfWeekRule(bool[] weekdayMask,
            IOpeningHoursRule containedRule)
        {
            _weekdayMask = weekdayMask;
            _containedRule = containedRule;
        }

        public override string ToString()
        {
            var selectedWeekdays = new List<string>();
            for (var i = 0; i < _weekdays.Count; i++)
            {
                if (_weekdayMask[i])
                {
                    selectedWeekdays.Add(_weekdays[i]);
                }
            }

            return $"{string.Join(",", selectedWeekdays)} {_containedRule}";
        }

        private bool InRange(DateTime moment)
        {
            return _weekdayMask[(6 + (int) moment.DayOfWeek) % 7];
            // Monday == 1, sunday == 0 according to dateTime;
            // whereas monday = 0 is the logical approach of course!
        }

        public DateTime NextChange(DateTime from)
        {
            while (!InRange(from))
            {
                from = from.Date.AddDays(1);
            }

            return _containedRule.NextChange(from);
        }

        public DateTime PreviousChange(DateTime from)
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

    public class MonthOfYearRule : IOpeningHoursRule
    {
        private static readonly List<string> _months =
            new List<string> {"jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"};

        // Why did the programmer confuse halloween and christmas?
        // Because oct 31 = dec 25

        private readonly bool[] _mask;
        private readonly IOpeningHoursRule _containedRule;

        public MonthOfYearRule(bool[] monthMask, IOpeningHoursRule containedRule)
        {
            _mask = monthMask;
            _containedRule = containedRule;
        }

        public override string ToString()
        {
            var selected = new List<string>();
            for (var i = 0; i < _months.Count; i++)
            {
                if (_mask[i])
                {
                    selected.Add(_months[i]);
                }
            }

            return $"{string.Join(",", selected)} {_containedRule}";
        }

        private bool InRange(DateTime moment)
        {
            return _mask[moment.Month - 1];
        }

        public DateTime NextChange(DateTime from)
        {
            while (!InRange(from))
            {
                from = from.Date.AddDays(1);
            }

            return _containedRule.NextChange(from);
        }

        public DateTime PreviousChange(DateTime from)
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
        private readonly IOpeningHoursRule _state;

        public HoursRule(TimeSpan start, TimeSpan stop, IOpeningHoursRule state)
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


        public override string ToString()
        {
            return $"{_start.Hours}:{_start.Minutes}-{_stop.Hours}:{_stop.Minutes} {_state}";
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
            return moment.TimeOfDay >= _start && moment.TimeOfDay < _stop
                ? _state.StateAt(moment)
                : null;
        }
    }

    public class PriorityRules : IOpeningHoursRule
    {
        public readonly List<IOpeningHoursRule> _rules;

        public PriorityRules(List<IOpeningHoursRule> rules)
        {
            _rules = rules;
        }


        public override string ToString()
        {
            return string.Join("; ", _rules);
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

    public class OsmState : IOpeningHoursRule
    {
        private readonly string _state;

        public OsmState(string state)
        {
            _state = state;
        }


        public override string ToString()
        {
            return "<osmstate: >" + _state;
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