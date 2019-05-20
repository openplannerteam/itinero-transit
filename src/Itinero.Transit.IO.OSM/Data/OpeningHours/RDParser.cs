using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming

namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    // I had a major headache while writing this code. Make sure you don't get one too

    /// <summary>
    /// </summary>
    public static class RdParsers
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

        /// <summary>
        /// Parse exactly the given string - case sensitive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static RDParser<string> Lit(string value)
        {
            return new RDParser<string>(
                str =>
                {
                    if (str.StartsWith(value))
                    {
                        return (value, str.Substring(value.Length));
                    }

                    return null;
                }, value);
        }


        /// <summary>
        /// Parse the given string, case insensitively
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static RDParser<string> LitCI(string value)
        {
            return new RDParser<string>(
                str =>
                {
                    if (str.Length < value.Length)
                    {
                        return null;
                    }

                    var start = str.Substring(0, value.Length);
                    if (start.ToLower().StartsWith(value.ToLower()))
                    {
                        return (start, str.Substring(value.Length));
                    }

                    return null;
                }, value);
        }

        /// <summary>
        /// Whitespace
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static RDParser<string> WS()
        {
            return Regex("[ \t]*");
        }


        public static RDParser<int> Int()
        {
            return Regex("-?[0-9]*").Map(int.Parse);
        }

        public static RDParser<string> Regex(string regex)
        {
            return new RDParser<string>(
                str =>
                {
                    Regex reg = new Regex($"^({regex})(.*)$");


                    var groups = reg.Matches(str);
                    if (groups.Count == 0)
                    {
                        return null;
                    }

                    var matches = groups[0].Groups;

                    if (matches.Count == 3)
                    {
                        // matches[0] is the entire string
                        return (matches[1].Value, matches[2].Value);
                    }

                    return null;
                }, regex);
        }
    }

    public class RDParser<T>
    {
        public readonly Func<string, (T, string)?> Parse;
        private readonly string _bnf;

        public RDParser(Func<string, (T, string)?> parse, string bnf)
        {
            Parse = parse;
            _bnf = bnf;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public RDParser(T value)
            : this(str => (value, str), "")
        {
        }

        public override string ToString()
        {
            return _bnf;
        }

        /// <summary>
        /// Casts the RDparser of T into a RDParser of Object, in order to ignore it later on.
        /// In other words, this operator does nothing useful, except to work around C# language constraints.
        /// </summary>
        /// <returns></returns>
        public static RDParser<object> operator !(RDParser<T> rdParser)
        {
            return rdParser.Map(x => (object) x);
        }

        /// <summary>
        /// Plugs in a default value when parsing failed
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> operator -(RDParser<T> rdParser, T value)
        {
            return rdParser | new RDParser<T>(value);
        }


        /// <summary>
        /// Discards the second element
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> operator +(RDParser<T> kept, RDParser<object> discarded)
        {
            return kept.Bind(t => discarded.Bind(_ => new RDParser<T>(t)));
        }

        /// <summary>
        /// Discards the first element
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> operator +(RDParser<object> discard, RDParser<T> keep)
        {
            return discard.Bind(_ => keep.Bind(t => new RDParser<T>(t)));
        }


        /// <summary>
        /// "OR"
        /// First tries the first parser, if that one fails, the second parser is taken
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> operator |(RDParser<T> a, RDParser<T> b)
        {
            return new RDParser<T>(
                str => a.Parse(str) ?? b.Parse(str),
                a._bnf + "|" + b._bnf);
        }

        /// <summary>
        /// Utility function: takes two parsers, parses one, then the other.
        /// Both values are passed into the 'f' function.
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> X<A, B>(
            Func<A, B, T> f,
            RDParser<A> parseA,
            RDParser<B> parseB
        )
        {
            return parseA.Bind(a => parseB.Bind(
                b => new RDParser<T>(f(a, b))));
        }

        /// <summary>
        /// Utility function: takes three parsers, parses one, then the second, then the last
        /// All values are passed into the 'f' function.
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> X<A, B, C>(
            Func<A, B, C, T> f,
            RDParser<A> parseA,
            RDParser<B> parseB,
            RDParser<C> parseC
        )
        {
            return parseA.Bind(
                a => parseB.Bind(
                    b => parseC.Bind(c =>
                        new RDParser<T>(f(a, b, c)))));
        }

        /// <summary>
        /// RECURSE
        /// Repeats the parser as much as possible, with the given separator in between.
        /// The current parser is executed at least once
        /// </summary>
        /// <returns></returns>
        public RDParser<List<T>> Unjoin(RDParser<string> seperator)
        {
            return RDParser<List<T>>.X(
                (t, ts) =>
                {
                    var l = new List<T>();
                    l.Add(t);
                    l.AddRange(ts);
                    return l;
                },
                this,
                (!seperator + this).R()
            );
        }

        /// <summary>
        /// RECURSE
        /// Repeats the parser as much as possible.
        /// Important: make sure the parser consumes at least on character or fails.
        /// Otherwise, R() will loop while parsing and eat all the stack
        /// </summary>
        /// <returns></returns>
        [Pure]
        public RDParser<List<T>> R()
        {
            return Recurse(new List<T>());
        }

        private RDParser<List<T>> Recurse(List<T> builtTillNow)
        {
            return this.Bind(t =>
                   {
                       var l = new List<T>();
                       l.AddRange(builtTillNow);
                       l.Add(t);
                       return Recurse(l);
                   }) |
                   new RDParser<List<T>>(builtTillNow);
        }


        [Pure]
        // ReSharper disable once MemberCanBePrivate.Global
        public RDParser<U> Bind<U>(Func<T, RDParser<U>> fa2Mb)
        {
            return new RDParser<U>(str =>
            {
                var raw = Parse(str);
                if (raw == null)
                {
                    return null;
                }

                var (t, rest) = raw.Value;
                return fa2Mb(t).Parse(rest);
            }, "");
        }


        [Pure]
        public RDParser<U> Map<U>(Func<T, U> f)
        {
            return new RDParser<U>(
                str =>
                {
                    var raw = Parse(str);
                    if (raw == null)
                    {
                        return null;
                    }

                    var (t, rest) = raw.Value;
                    return (f(t), rest);
                }, _bnf
            );
        }
    }
}