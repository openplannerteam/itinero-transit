using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming


namespace Itinero.Transit.IO.OSM.Data.Parser
{
    // I had a major headache while writing this code. Make sure you don't get one too


    /// <summary>
    /// A collection of generally useful small parsers, such as 'regex', 'literal', 'number', ...
    /// </summary>
    public static class DefaultRdParsers
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
                input =>
                {
                    var str = input.Item1;
                    var index = input.Item2;
                    if (!str.StartsWith(value))
                        return ParseResult<string>.Failed(input, $"Expected a literal '{value}'");
                    var l = (uint) value.Length;
                    return new ParseResult<string>(
                        str.Substring(value.Length),
                        index + l,
                        value
                    );
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
                input =>
                {
                    var str = input.Item1;
                    var index = input.Item2;
                    if (str.Length < value.Length)
                    {
                        return ParseResult<string>.Failed(input,
                            $"Expected a case insensitive literal: '{value}', but the input string has not enough characters left");
                    }

                    var start = str.Substring(0, value.Length);
                    if (!start.ToLower().StartsWith(value.ToLower()))
                    {
                        return ParseResult<string>.Failed(input,
                            $"Expected a case insensitive literal: '{value}'");
                    }

                    var l = (uint) value.Length;
                    return new ParseResult<string>(str.Substring((int) l), index + l, start);
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


        [Pure]
        public static RDParser<int> Int()
        {
            return Regex("-?[0-9]*").Map(int.Parse);
        }

        [Pure]
        public static RDParser<double> Double()
        {
            return
                (Regex("-?[0-9]+\\.[0-9]+") |
                 Regex("-?[0-9]+") |
                 Regex("-?\\.[0-9]+"))
                .Map(str => double.Parse(str, CultureInfo.InvariantCulture));
        }

        [Pure]
        public static RDParser<string> Regex(string regex)
        {
            return new RDParser<string>(
                input =>
                {
                    var str = input.Item1;
                    var index = input.Item2;
                    var reg = new Regex($"^({regex})(.*)$");


                    var groups = reg.Matches(str);
                    if (groups.Count == 0)
                    {
                        return ParseResult<string>.Failed(input, $"Regex {regex} could not be matched");
                    }

                    var matches = groups[0].Groups;

                    if (matches.Count != 3)
                        return ParseResult<string>.Failed(input, $"Regex {regex} could not be matched");


                    // matches[0] is the entire string
                    var match = matches[1].Value;
                    var rest = matches[2].Value;
                    return new ParseResult<string>(rest, index + (uint) match.Length, match);
                }, regex);
        }


        [Pure]
        public static RDParser<T> Fail<T>(string message)
        {
            return new RDParser<T>(
                str => ParseResult<T>.Failed(str, message), ""
            );
        }
    }

    /// <summary>
    /// Either an error message and location or an result
    /// </summary>
    public struct ParseResult<T>
    {
        public uint Index { get; }
        public string ErrorMessage { get; }
        public T Result { get; }
        public string Rest { get; }

        public ParseResult(string rest, uint index, T value)
        {
            Index = index;
            ErrorMessage = null;
            Result = value;
            Rest = rest;
        }


        private ParseResult(string rest, uint index, string errorMessage, T value)
        {
            Index = index;
            ErrorMessage = errorMessage;
            Result = value;
            Rest = rest;
        }

        public static ParseResult<T> Failed((string rest, uint index) restIndex, string errorMessage)
        {
            return new ParseResult<T>(restIndex.rest, restIndex.index, errorMessage, default(T));
        }

        public ParseResult<T0> Map<T0>(Func<T, T0> f)
        {
            return Success()
                ? new ParseResult<T0>(Rest, Index, ErrorMessage, f(Result))
                : Fail<T0>();
        }


        public ParseResult<T0> FailAmendMessage<T0>(string message)
        {
            return new ParseResult<T0>(Rest, Index, message + ErrorMessage, default(T0));
        }

        public string FancyErrorMessage()
        {
            return $"At position {Index}: {ErrorMessage}";
        }

        public ParseResult<T0> Fail<T0>()
        {
            return new ParseResult<T0>(Rest, Index, ErrorMessage, default(T0));
        }


        public bool Success()
        {
            return string.IsNullOrEmpty(ErrorMessage);
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
        public readonly Func<(string rest, uint index), ParseResult<T>> Parse;
        private readonly string _bnf;

        public RDParser(Func<(string, uint), ParseResult<T>> parse, string bnf)
        {
            Parse = parse;
            _bnf = bnf;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public RDParser(T value)
        {
            Parse = restIndex =>
                new ParseResult<T>(restIndex.rest, restIndex.index, value);
            _bnf = "";
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
        /// Discards the first element.
        /// </summary>
        /// <returns></returns>
        public static RDParser<T> operator *(RDParser<object> discard, RDParser<T> keep)
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
                input =>
                {
                    var resultA = a.Parse(input);
                    if (resultA.Success())
                    {
                        return resultA;
                    }

                    var resultB = b.Parse(input);
                    if (resultB.Success())
                    {
                        return resultB;
                    }

                    return ParseResult<T>.Failed(
                        input, "Both options failed:\n" + resultA.ErrorMessage + "\n" + resultB.ErrorMessage
                    );
                },
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

        public static RDParser<(A, B)> X<A, B>(
            RDParser<A> parseA,
            RDParser<B> parseB
        )
        {
            return parseA.Bind(a => parseB.Bind(
                b => new RDParser<(A, B)>((a, b))));
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
                (!seperator * this).R()
            );
        }


        /// <summary>
        /// Parses the given input string, crashes if the entire string was not entirely consumed
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

            var result = Parse((value, 0));


            if (!result.Success())
            {
                throw new FormatException(
                    $"{errormsg}: could not parse {value}: {result.ErrorMessage})");
            }

            if (!string.IsNullOrEmpty(result.Rest))
            {
                throw new FormatException(
                    $"{errormsg}: could not parse everything of {value}, only {result.Index} characters parsed");
            }

            return result.Result;
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
                if (!raw.Success())
                {
                    return raw.Fail<U>();
                }

                var t = raw.Result;
                return fa2Mb(t).Parse((raw.Rest, raw.Index));
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
                v => Parse(v).Map(f), _bnf);
        }


        [Pure]
        public RDParser<T> Assert(Predicate<T> predicate, string msg)
        {
            return Bind(t =>
                predicate(t) ? new RDParser<T>(t) : DefaultRdParsers.Fail<T>("Predicate failed: " + msg)
            );
        }
    }
}