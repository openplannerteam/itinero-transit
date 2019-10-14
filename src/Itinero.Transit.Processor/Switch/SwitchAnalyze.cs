using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchAnalyze : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--analyse", "--analyze"};

        private static string _about =
            "Given an output directory with .csv files generated earlier, calculates all kinds of fun statistics.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("directory", "dir",
                            "The directory where all the .csv-files are located")
                        .SetDefault("."),
                    SwitchesExtensions.opt("keys",
                            "The keys to analyze, comma-seperated")
                        .SetDefault("calculationTime (ms)"),
                };

        private const bool _isStable = SwitchCalculateAll._isStable && false;

        public SwitchAnalyze() :
            base(_names, _about, _extraParams, _isStable)
        {
        }

        public void Use(Dictionary<string, string> parameters, TransitDb _)
        {
            var (data, keys) = AllData(parameters["directory"]);

            var keysParam = parameters["keys"];
            var keysToAnalyze =
                keysParam.Equals("*") ? keys : keysParam.Split(",").Select(k => k.Trim()).ToList();
            foreach (var k in keysToAnalyze)
            {
                Analyze(data, k.Trim(), keys);
            }
        }

        private void Analyze(List<Dictionary<string, string>> data, string key, List<string> keys)
        {
            double min = double.MaxValue;
            Dictionary<string, string> minEntry = null;
            Dictionary<string, string> maxEntry = null;
            double max = double.MinValue;
            double sum = 0;

            uint emptyCount = 0;

            var allValues = new List<double>();

            for (var i = 0; i < data.Count; i++)
            {
                var entry = data[i];
                try
                {
                    if (!entry.ContainsKey(key))
                    {
                        emptyCount++;
                        continue;
                    }

                    var str = entry[key];
                    if (string.IsNullOrEmpty(str))
                    {
                        emptyCount++;
                        continue;
                    }

                    var v = double.Parse(entry[key]);
                    allValues.Add(v);
                    sum += v;
                    if (v < min)
                    {
                        min = v;
                        minEntry = entry;
                    }

                    if (v > max)
                    {
                        max = v;
                        maxEntry = entry;
                    }
                }
                catch
                {
                    Console.WriteLine($"Analyzing {key} failed - column probably is not a number");
                    return;
                }
            }

            Console.WriteLine($"\rOverview for key {key}:\n" +
                              $"Total entries count: {data.Count}\n" +
                              $"Of which empty: {emptyCount}\n" +
                              $"Sum: {sum}\n" +
                              $"Avg: {sum / data.Count:F3}\n" +
                              $"Max: {max}\n{EntryToString(maxEntry, keys)}\n" +
                              $"Min: {min}\n{EntryToString(minEntry, keys)}\n\n");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (max == Double.MinValue
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                || min == Double.MaxValue)
            {
                return;
            }

            Console.WriteLine(CreateHistogram(allValues, key, min, max));
        }

        private string CreateHistogram(IEnumerable<double> all, string key, double min, double max,
            uint bucketCount = 50, uint width = 120)
        {
            var buckets = new uint[bucketCount + 1];
            foreach (var d in all)
            {
                var bucket = (int) Math.Floor(bucketCount * (d - min) / (max - min));
                buckets[bucket]++;
            }

            var nameLength = (uint) Math.Log10(max);
            var biggestCount = buckets.Max();

            var result = $"Histogram for {key} with range {min} --> {max}:\n\n";
            var singleBucketSize = (max - min) / bucketCount;
            for (var i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket == 0)
                {
                    continue;
                }

                var top = (uint) (i + 1) * singleBucketSize + min;
                var name = Format((int) top, nameLength);

                var factor = width * bucket / biggestCount;
                var progressBar = "";

                for (var j = 0; j < width; j++)
                {
                    progressBar += (j <= factor ? "-" : " ");
                }

                result += $"Up to {name} has {Format((int) bucket, (uint) Math.Log10(biggestCount))} [{progressBar}]\n";
            }

            return result;
        }

        private string Format(int d, uint length)
        {
            string s = "" + d;
            while (s.Length <= length)
            {
                s = " " + s;
            }

            return s;
        }

        private string EntryToString(Dictionary<string, string> entry, List<string> keys)
        {
            if (entry == null)
            {
                return "{}";
            }

            var result = "    {\n";
            foreach (var key in keys)
            {
                if (entry.TryGetValue(key, out var val))
                {
                    result += $"     {key}:{val}\n";
                }
                else
                {
                    result += $"     {key}:null\n";
                }
            }

            return result + "    }";
        }

        private (List<Dictionary<string, string>> entries, List<string> keys) AllData(string directory)
        {
            var dir = new DirectoryInfo(directory);
            var entries = new List<Dictionary<string, string>>();

            List<string> keys = null;
            foreach (var f in dir.GetFiles("*.csv"))
            {
                var data = File.ReadAllLines(f.FullName);
                keys = data[0].Split(",")
                    .Select(k => k.Trim()).ToList();

                for (int i = 1; i < data.Length; i++)
                {
                    var entryData = data[i].Split(",");
                    var entry = new Dictionary<string, string>();
                    for (var j = 0; j < entryData.Length; j++)
                    {
                        var key = keys[j];
                        entry[key] = entryData[j];
                    }

                    entries.Add(entry);
                }
            }

            return (entries, keys);
        }
    }
}