using System.Diagnostics.Contracts;

namespace Itinero.Transit.IO.OSM.Data.Parser
{
    /// <summary>
    /// This class implements Opening Hours Rules parsing, as declared by
    /// https://wiki.openstreetmap.org/wiki/Key:opening_hours#Syntax
    /// or by
    /// https://openingh.ypid.de/netzwolf_mirror/time_domain/specification.html#rule7
    /// </summary>
    public static class OpeningHoursRuleParser 
    {
        // ---------------- CALENDAR -------------------------


        [Pure]
        public static RDParser<ITimedElement> Date()
        {
            return
                // THe very good 'Sep 01'
                RDParser<ITimedElement>.X(
                    (m, d) => TimedElements.Date(m, (int) d),
                    Month(), DayNum()
                ) 
                |
                // The somewhat naughthy 'Sep Su [1]' and friends 'Oct Sa [-1]', 'Nov Mo [2,4]'
                RDParser<ITimedElement>.X(
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
        public static RDParser<ITimedElement> VariableDate()
        {
            return DefaultRdParsers.LitCI("easter").Map(
                _ => TimedElements.Easter()
            );
        }


        // ----------------- BASIC ELEMENTS -------------------
        [Pure]
        public static RDParser<uint> Minute()
        {
            return DefaultRdParsers.Int().Assert(i => i >= 0 && i <= 60, "Minute is out of range").Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<uint> Hour()
        {
            return DefaultRdParsers.Int().Assert(i => i >= 0 && i <= 24, "Hour is out of range").Map(i => (uint) i);
        }


        [Pure]
        public static RDParser<uint> MDay()
        {
            return DayNum() + !DefaultRdParsers.Lit(".");
        }

        [Pure]
        public static RDParser<uint> DayNum()
        {
            return DefaultRdParsers.Int()
                .Assert(i => i >= 1 && i <= 31, "Day of month is out of range, should be between 0 and 31").Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<uint> WeekNum()
        {
            return DefaultRdParsers.Int()
                .Assert(i => i >= 1 && i <= 53, "Weeknum is out of range, should be between 1 and 53").Map(i => (uint) i);
        }

        [Pure]
        public static RDParser<int> Wday()
        {
            return DefaultRdParsers.LitCI("mo").Map(_ => 0) |
                   DefaultRdParsers.LitCI("tu").Map(_ => 1) |
                   DefaultRdParsers.LitCI("we").Map(_ => 2) |
                   DefaultRdParsers.LitCI("th").Map(_ => 3) |
                   DefaultRdParsers.LitCI("fr").Map(_ => 4) |
                   DefaultRdParsers.LitCI("sa").Map(_ => 5) |
                   DefaultRdParsers.LitCI("su").Map(_ => 6);
        }

        [Pure]
        // ReSharper disable once MemberCanBePrivate.Global
        public static RDParser<int> Month()
        {
            return DefaultRdParsers.LitCI("jan").Map(_ => 1) |
                   DefaultRdParsers.LitCI("feb").Map(_ => 2) |
                   DefaultRdParsers.LitCI("mar").Map(_ => 3) |
                   DefaultRdParsers.LitCI("apr").Map(_ => 4) |
                   DefaultRdParsers.LitCI("may").Map(_ => 5) |
                   DefaultRdParsers.LitCI("jun").Map(_ => 6) |
                   DefaultRdParsers.LitCI("jul").Map(_ => 7) |
                   DefaultRdParsers.LitCI("aug").Map(_ => 8) |
                   DefaultRdParsers.LitCI("sep").Map(_ => 9) |
                   DefaultRdParsers.LitCI("oct").Map(_ => 10) |
                   DefaultRdParsers.LitCI("nov").Map(_ => 11) |
                   DefaultRdParsers.LitCI("dec").Map(_ => 12);
        }

        [Pure]
        public static RDParser<(uint, uint)> HourMinutes()
        {
            return RDParser<(uint, uint)>.X(
                (h, m) => (h, m),
                Hour() + !DefaultRdParsers.Lit(": "), Minute()
            );
        }

        [Pure]
        public static RDParser<string> Comment()
        {
            return DefaultRdParsers.Regex("\"[^\"]*\"");
        }
    }
}