using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Itinero.Transit.Processor
{
    public static class SwitchesExtensions
    {
        public static (List<string> args, bool isObligated, string comment, string defaultValue) obl(string argName,
            string comment)
        {
            return (new List<string> {argName}, true, comment, "");
        }

        public static (List<string> args, bool isObligated, string comment, string defaultValue) obl(string argName,
            string argName0, string comment)
        {
            return (new List<string> {argName, argName0}, true, comment, "");
        }
        
        public static (List<string> args, bool isObligated, string comment, string defaultValue) obl(string argName,
            string argName0, string argname1, string comment)
        {
            return (new List<string> {argName, argName0, argname1}, true, comment, "");
        }


        public static (List<string> args, bool isObligated, string comment, string defaultValue) opt(string argName,
            string comment)
        {
            return (new List<string> {argName}, false, comment, "");
        }

        public static (List<string> args, bool isObligated, string comment, string defaultValue) opt(string argName,
            string argName0, string comment)
        {
            return (new List<string> {argName, argName0}, false, comment, "");
        }
        

        public static (List<string>argName, bool isObligated, string comment, string defaultValue) SetDefault(
            this (List<string> args, bool isObligated, string comment, string def) tuple, string defaultValue)
        {
            return (tuple.args, tuple.isObligated, tuple.comment, defaultValue);
        }

        public static bool Bool(this Dictionary<string, string> dict, string key)
        {
            return dict.Map(key, bool.Parse);
        }
        
        public static int Int(this Dictionary<string, string> dict, string key)
        {
            return dict.Map(key, int.Parse);
        }
        
        public static DateTime Date(this Dictionary<string, string> dict, string key)
        {
            return dict.Map(key, value => DateTime.Parse(value).ToUniversalTime());
        }

        public static T Map<T>(this Dictionary<string, string> dict, string key, Func<string, T> parser)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                return default(T);
            }

            try
            {

                return parser(value);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Error in argument '{key}': {e.Message}");
            }
        }
    }
}