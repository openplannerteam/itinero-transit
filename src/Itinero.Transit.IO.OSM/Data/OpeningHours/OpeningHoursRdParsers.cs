using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    /// <summary>
    /// </summary>
    public class OpeningHoursRdParsers  : DefaultRdParsers // Inheritance is purely to get all the functions without overhead
    {
        [Pure]
        public static RDParser<bool[]> DayOfWeekRange()
        {
            return TimeSpecification(DayOfWeek(), 7);
        }

        [Pure]
        public static RDParser<bool[]> MonthOfYearRange()
        {
            return TimeSpecification(MonthOfYear(), 12);
        }

        [Pure]
        private static RDParser<bool[]> TimeSpecification(RDParser<int> subparser, int range)
        {
            return MaskElement(subparser, range).Unjoin(Lit(",") + !WS()).Map
            (masks =>
            {
                var m = masks[0];
                for (var i = 1; i < masks.Count; i++)
                {
                    for (var j = 0; j < range; j++)
                    {
                        // Merge the bitmasks, where the bit is true if the shop is open that given day
                        m[j] |= masks[i][j];
                    }
                }

                return m;
            });
        }

        [Pure]
        private static RDParser<bool[]> MaskElement(RDParser<int> subParser, int range)
        {
            return RangeParser(subParser, range) |
                   subParser.Map(i =>
                   {
                       var dow = new bool[range];
                       dow[i] = true;
                       return dow;
                   });
        }

        /// <summary>
        /// Parse things as 'mo-fr' (with subParser = DayOfWeek) or 'Jan-Dec' (with subParser = MonthOfYear)
        /// </summary>
        /// <param name="subParser"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        [Pure]
        private static RDParser<bool[]> RangeParser(RDParser<int> subParser, int range)
        {
            return RDParser<bool[]>.X(
                (start, end) =>
                {
                    var mask = new bool[range];
                    for (int i = start; i % range != end; i++)
                    {
                        mask[i % range] = true;
                    }

                    mask[end % range] = true; // Include the last day

                    return mask;
                },
                subParser + !Lit("-"), subParser);
        }


        [Pure]
        public static RDParser<int> DayOfWeek()
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
        public static RDParser<int> MonthOfYear()
        {
            return LitCI("jan").Map(_ => 0) |
                   LitCI("feb").Map(_ => 1) |
                   LitCI("mar").Map(_ => 2) |
                   LitCI("apr").Map(_ => 3) |
                   LitCI("may").Map(_ => 4) |
                   LitCI("jun").Map(_ => 5) |
                   LitCI("jul").Map(_ => 6) |
                   LitCI("aug").Map(_ => 7) |
                   LitCI("sep").Map(_ => 8) |
                   LitCI("oct").Map(_ => 9) |
                   LitCI("nov").Map(_ => 10) |
                   LitCI("dec").Map(_ => 11);
        }

        [Pure]
        public static RDParser<IOpeningHoursRule> TwentyFourSeven()
        {
            return (
                !(Lit("24/7") | Lit("24/24") | Lit("7/7")) +
                (!WS() +
                 OSMStatus())
            );
        }

        [Pure]
        public static RDParser<IOpeningHoursRule> MonthRule()
        {
            return
                RDParser<IOpeningHoursRule>.X(
                    (weekdays, state) => new MonthOfYearRule(weekdays, state),
                    MonthOfYearRange() + !WS(),
                    WeekdayRule() | HoursRule()
                );
        }

        /// <summary>
        /// Parses multiple, ';'-seperated OpeningHour rules
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<IOpeningHoursRule> MultipleRules()
        {
            
            return (
                    WeekdayRule() | HoursRule() | OSMStatus()
                ).Unjoin(Lit(";")+!WS())
                .Map(rules =>  (IOpeningHoursRule) new PriorityRules(rules));

        }

        [Pure]
        public static RDParser<IOpeningHoursRule> WeekdayRule()
        {
            return
                RDParser<IOpeningHoursRule>.X(
                    (weekdays, state) => new DaysOfWeekRule(weekdays, state),
                    DayOfWeekRange() + !WS(),
                    HoursRule() | OSMStatus()
                );
        }

        /// <summary>
        /// Parse an OSM moment in time
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<IOpeningHoursRule> HoursRule()
        {
            return RDParser<IOpeningHoursRule>.X(
                (openHours, state) =>
                {
                    if (openHours.Count == 1)
                    {
                        return new HoursRule(openHours[0].start, openHours[0].end, state);
                    }

                    var rules = new List<IOpeningHoursRule>();

                    foreach (var (start, end) in openHours)
                    {
                        rules.Add(new HoursRule(start, end, state));
                    }

                    return new PriorityRules(rules);
                },
                OpenHours().Unjoin(Lit(",") + !WS()),
                !WS() + OSMStatus()
            );
        }


        [Pure]
        public static RDParser<(TimeSpan start, TimeSpan end)> OpenHours()
        {
            return RDParser<(TimeSpan, TimeSpan)>.X(
                (open, close) => (open, close),
                Moment() + !Lit("-"),
                Moment()
            );
        }

        /// <summary>
        /// Parse an OSM moment in time
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<TimeSpan> Moment()
        {
            return RDParser<TimeSpan>.X(
                (h, m) => new TimeSpan(h, m, 0),
                Int() + !Lit(":"), Int()
            );
        }


        /// <summary>
        /// Parses an OSM status, such as 'open', 'unknown', 'closed' or a comment enclosed by double quotes such as '"by appointment"'
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<IOpeningHoursRule> OSMStatus()
        {
            return ((LitCI("open") | LitCI("close") | LitCI("unknown") | LitCI("off") |
                     Regex("\"[^\"]*\"")) - "").Map(state => (IOpeningHoursRule) new OsmState(state));
        }
    }
}