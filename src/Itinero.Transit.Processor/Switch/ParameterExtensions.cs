using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    public static class ParameterExtensions
    {
        public static int GetInt(this Dictionary<string, string> parameters, string name)
        {
            return int.Parse(parameters[name]);
        }

        public static IEnumerable<string> GetFilesMatching(this Dictionary<string, string> parameters, string name)
        {
            var pattern = parameters[name];
            var files = Directory.EnumerateFiles(".", pattern).ToList();
            if (!files.Any())
            {
                throw new ArgumentException($"No files were found for the pattern {pattern} of argument {name}");
            }

            return files;
        }

        public static DateTime ParseDate(this Dictionary<string, string> parameters, string name)
        {
            var dateTime = parameters[name];
            if (dateTime.Equals("now"))
            {
                return DateTime.Now.ToUniversalTime();
            }

            if (dateTime.Equals("today"))
            {
                return DateTime.Now.Date.ToUniversalTime();
            }


            if (!dateTime.Contains("/"))
            {
                return new DateTime(DateTime.Parse(dateTime).Ticks, DateTimeKind.Utc);
            }

            var splitted = dateTime.Split("/");
            var parsed = new DateTime(DateTime.Parse(splitted[0]).Ticks, DateTimeKind.Unspecified);
            var region = splitted[1] + "/" + splitted[2];
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(region);
            return parsed.ConvertToUtcFrom(timezone);
        }

        public static int ParseTimeSpan(this Dictionary<string, string> parameters, string name, DateTime startTime)
        {
            try
            {
                var endDate = parameters.ParseDate(name);
                return (int) (endDate - startTime).TotalSeconds;
            }
            catch
            {
                return ParseTimeSpan(parameters, name);
            }
        }

        private static readonly IReadOnlyList<(string timeEntity, int factor)> _unitLengths
            = new List<(string timeEntity, int factor)>
            {
                ("minute", 60),
                ("minutes", 60),
                ("hour", 60 * 60),
                ("hours", 60 * 60),
                ("day", 24 * 60 * 60),
                ("days", 24 * 60 * 60),
                ("week", 7 * 24 * 60 * 60),
                ("weeks", 7 * 24 * 60 * 60),
            };

        /// <summary>
        /// Gives a timespan in seconds
        /// </summary>
        public static int ParseTimeSpan(this Dictionary<string, string> parameters, string name)
        {
            var durationStr = parameters[name];

            foreach (var (unitName, factor) in _unitLengths)
            {
                if (!durationStr.EndsWith(unitName)) continue;

                durationStr = durationStr.Substring(0, durationStr.Length - unitName.Length);
                return factor * int.Parse(durationStr);
            }

            return int.Parse(durationStr);
        }


        public static bool ParseBool(this Dictionary<string, string> parameters, string name)
        {
            return bool.Parse(parameters[name]);
        }
    }
}