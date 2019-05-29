using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming


namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    // I had a major headache while writing this code. Make sure you don't get one too


    /// <summary>
    /// A collection of generally useful small parsers, such as 'regex', 'literal', 'number', ...
    /// </summary>
    public class DefaultRdParsers
    {
        [Pure]
        public static RDParser<TimeSpan> Duration()
        {
            return
                RDParser<TimeSpan>.X(
                    (h, m, s) => new TimeSpan(h, m, s),
                    Int() + !Lit(":"), Int() + !Lit(":"), Int())
                | RDParser<TimeSpan>.X(
                    (h, m) => new TimeSpan(h, m, 0),
                    Int() + !Lit(":"), Int())
                | (Int() + !(Lit("hours") | Lit("hour") | Lit("h"))).Map(m => TimeSpan.FromHours(m))
                | (Int() + !(Lit("minutes") | Lit("min") | Lit("m") | Lit(""))).Map(m => TimeSpan.FromMinutes(m));
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


        [Pure]
        public static RDParser<T> Fail<T>()
        {
            return new RDParser<T>(
                str => null, ""
            );
        }
    }

    /// <summary>
    /// This is a basic, Recursive Descent parser.
    /// This uses a lot of functional magic.
    /// Basically, an RDParser is an object which contains a function 'Parse'.
    /// "Parse", when called, will take a string and produce in return a value (of type T) and a nonconsumed string (or null, if parsing failed)
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        /// 
        /// </summary>
        /// <returns></returns>
        public T ParseFull(string value, string errormsg = null)
        {
            if (value == null)
            {
                if (errormsg != null)
                {
                    throw new FormatException(errormsg + ": input is null");
                }

                return default(T);
            }

            var raw = Parse(value);
            if (raw == null)
            {
                throw new FormatException($"{errormsg}: could not parse {value}");
            }

            var (t, rest) = raw.Value;

            if (!string.IsNullOrEmpty(rest))
            {
                throw new FormatException($"{errormsg}: did not parse completely (input: {value}, rest: {rest})");
            }

            return t;
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
            return Bind(t =>
                   {
                       var l = new List<T>();
                       l.AddRange(builtTillNow);
                       l.Add(t);
                       return Recurse(l);
                   }) |
                   new RDParser<List<T>>(builtTillNow);
        }


        /// <summary>
        /// Monadic bind.
        /// Quite a special function with quite cool functionalities.
        /// </summary>
        /// <param name="fa2Mb"></param>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Apply the given function f onto the parsed value
        /// </summary>
        /// <param name="f"></param>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
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


        [Pure]
        public RDParser<T> Assert(Predicate<T> predicate)
        {
            return Bind(t => predicate(t) ? new RDParser<T>(t) : DefaultRdParsers.Fail<T>()
            );
        }
    }
}