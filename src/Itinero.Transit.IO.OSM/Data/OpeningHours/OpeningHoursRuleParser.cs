using System;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    /// <summary>
    /// This class implements Opening Hours Rules parsing, as declared by
    /// https://wiki.openstreetmap.org/wiki/Key:opening_hours#Syntax
    /// or by
    /// https://openingh.ypid.de/netzwolf_mirror/time_domain/specification.html#rule7
    /// </summary>
    public class OpeningHoursRuleParser : DefaultRdParsers
    {
        // ---------------- CALENDAR -------------------------


        [Pure]
        public static RDParser<TimedElement> Date()
        {
            return
                // THe very good 'Sep 01'
                RDParser<TimedElement>.X(
                    (m, d) => TimedElements.Date(m, (int) d),
                    Month(), DayNum()
                ) 
                |
                // The somewhat naughthy 'Sep Su [1]' and friends 'Oct Sa [-1]', 'Nov Mo [2,4]'
                RDParser<TimedElement>.X(
                    (m, wd) => TimedElements.MonthOfYear(m).Chain(
                        new DayOfWeekEvent(wd)),
                    Month(), Wday())
                |
                VariableDate();
        }


        /// <summary>
        /// Parse a 'variable date', most notable 'easter'.
        /// THe result will be a function which maps the year onto the datetime
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<TimedElement> VariableDate()
        {
            return LitCI("easter").Map(
                _ => TimedElements.Easter()
            );
        }


        // ----------------- BASIC ELEMENTS -------------------
        [Pure]
        public static RDParser<uint> Minute()
        {
            return Int().Assert(i => i >= 0 && i <= 60).Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<uint> Hour()
        {
            return Int().Assert(i => i >= 0 && i <= 24).Map(i => (uint) i);
        }


        [Pure]
        public static RDParser<uint> MDay()
        {
            return DayNum() + !Lit(".");
        }

        [Pure]
        public static RDParser<uint> DayNum()
        {
            return Int()
                .Assert(i => i >= 1 && i <= 31).Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<uint> WeekNum()
        {
            return Int()
                .Assert(i => i >= 1 && i <= 53).Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<int> Wday()
        {
            return LitCI("mo").Map(_ => 0) |
                   LitCI("tu").Map(_ => 1) |
                   LitCI("we").Map(_ => 2) |
                   LitCI("th").Map(_ => 3) |
                   LitCI("fr").Map(_ => 4) |
                   LitCI("sa").Map(_ => 5) |
                   LitCI("su").Map(_ => 6);
        }

        [Pure]
        // ReSharper disable once MemberCanBePrivate.Global
        public static RDParser<int> Month()
        {
            return LitCI("jan").Map(_ => 1) |
                   LitCI("feb").Map(_ => 2) |
                   LitCI("mar").Map(_ => 3) |
                   LitCI("apr").Map(_ => 4) |
                   LitCI("may").Map(_ => 5) |
                   LitCI("jun").Map(_ => 6) |
                   LitCI("jul").Map(_ => 7) |
                   LitCI("aug").Map(_ => 8) |
                   LitCI("sep").Map(_ => 9) |
                   LitCI("oct").Map(_ => 10) |
                   LitCI("nov").Map(_ => 11) |
                   LitCI("dec").Map(_ => 12);
        }

        [Pure]
        public static RDParser<(uint, uint)> HourMinutes()
        {
            return RDParser<(uint, uint)>.X(
                (h, m) => (h, m),
                Hour() + !Lit(": "), Minute()
            );
        }

        [Pure]
        public static RDParser<string> Comment()
        {
            return Regex("\"[^\"]*\"");
        }
    }
}