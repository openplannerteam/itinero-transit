using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchCalculateAll : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--calculate-all"};

        private static string _about =
            "Calculates all possible journeys from one (or all) locations to one (or all) other locations. Either the individual journeys are written to a .csv-file or statistics are calculated.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("file",
                            "The file to write the data to, in .csv format. The strings '{from}' and '{to}' in the name will be replaced by the actual name. If they are not given, the results will instead be appended")
                        .SetDefault("output-{from}.csv"),
                    SwitchesExtensions.opt("from", "The URI of the departure station. Use * for all")
                        .SetDefault("*"),
                    SwitchesExtensions.opt("to", "The URI of the arrival station. Use * for all")
                        .SetDefault("*"),
                    SwitchesExtensions.opt("summarize", "If this flag is set, only statistics are kept.")
                        .SetDefault("false"),
                    SwitchesExtensions.opt("departureTime",
                        "The earliest allowed departure time. If unset, the entire time of the database will be used"),
                    SwitchesExtensions.opt("arrivalTime",
                        "The latest allowed arrival time. If unset, the entire time of the database will be used"),
                    SwitchesExtensions.opt("startAt",
                            "[integer] If specified, only start at departure stop 'startAt'. Used to partition the calculations on mutliple threads")
                        .SetDefault("0"),
                    SwitchesExtensions.opt("stopAt",
                            "[integer] If specified, stop at departure stop 'stopAt'. Used to partition the calculations on multiple threads")
                        .SetDefault(int.MaxValue.ToString()),
                    SwitchesExtensions.opt("skip",
                            "[integer] If specified, only calculate (startAt + n * skip). Used to partition the calculations on multiple threads")
                        .SetDefault("0"),
                };

        private const bool _isStable = true;


        public SwitchCalculateAll
            () :
            base(_names, _about, _extraParams, _isStable)
        {
        }

        public void Use(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var writeTo = arguments["file"];

            var from = arguments["from"];
            var to = arguments["to"];
            var summarize = bool.Parse(arguments["summarize"]);
            var startAt = int.Parse(arguments["startAt"]);
            var stopAt = int.Parse(arguments["stopAt"]);
            var skip = int.Parse(arguments["skip"]);

            var snapshot = tdb.Latest;

            var departureTime = snapshot.ConnectionsDb.EarliestDate;
            if (!arguments["departureTime"].Equals(""))
            {
                departureTime =
                    DateTimeExtensions.ToUnixTime(DateTime.Parse(arguments["departureTime"]).ToUniversalTime());
            }

            var arrivalTime = snapshot.ConnectionsDb.LatestDate;
            if (!arguments["arrivalTime"].Equals(""))
            {
                arrivalTime = DateTimeExtensions.ToUnixTime(DateTime.Parse(arguments["arrivalTime"]).ToUniversalTime());
            }


            // ----- Actual calculations below ------ 


            var reader = tdb.Latest.StopsDb.GetReader();

            var profile = new DefaultProfile(0, 0);
            var withProfile = tdb.SelectProfile(profile);

            reader.Reset();
            var count = 0;
            while (reader.MoveNext())
            {
                count++;
            }


            if (from.Equals("*"))
            {
                var readerFrom = tdb.Latest.StopsDb.GetReader();
                readerFrom.Reset();
                var i = 0;
                while (readerFrom.MoveNext())
                {
                    i++;
                    if (i < startAt)
                    {
                        continue;
                    }

                    if (i >= stopAt)
                    {
                        break;
                    }

                    Console.WriteLine($"\rCalculating departure stop {i}/{count}");
                    CalculateWithFixedFrom(readerFrom.GlobalId);
                    for (int j = 1; j < skip; j++)
                    {
                        i++;
                        readerFrom.MoveNext();
                    }
                }
            }
            else
            {
                CalculateWithFixedFrom(@from);
            }


            void CalculateWithFixedFrom(string fixedFrom)
            {
                var foundJourneys = 0;
                var start = DateTime.Now;
                if (to.Equals("*"))
                {
                    reader.Reset();
                    var i = 0;
                    while (reader.MoveNext())
                    {
                        i++;
                        var secNeeded = (DateTime.Now - start).TotalSeconds;
                        var iStr = i.ToString("D" + (uint) Math.Log10(count * 10));
                        var avg = secNeeded / i;
                        var perc = 80 * i / count;
                        var percStr = "";
                        for (int j = 0; j < 80; j++)
                        {
                            percStr += (j < perc) ? "-" : " ";
                        }

                        Console.Write(
                            $"\r{iStr}/{count} {foundJourneys} journeys, {secNeeded:F1}s, avg {avg:F3}s, eta {(int) (avg * (count - i))}s [{percStr}]    ");
                        if (reader.GlobalId.Equals(fixedFrom))
                        {
                            continue;
                        }

                        var withArgs = new WithArguments(reader, fixedFrom, reader.GlobalId, summarize);
                        var calculator =
                            withProfile.SelectStops(fixedFrom, reader.GlobalId)
                                .SelectTimeFrame(departureTime, arrivalTime);

                        foundJourneys += CalculateSingleEntry(fixedFrom, reader.GlobalId, calculator, withArgs);
                    }

                    Console.Write(
                        "\r                                                                                                                                     ");
                }
                else
                {
                    var withArgs = new WithArguments(reader, fixedFrom, to, summarize);
                    var calculator =
                        withProfile.SelectStops(fixedFrom, to)
                            .SelectTimeFrame(departureTime, arrivalTime);


                    CalculateSingleEntry(fixedFrom, to, calculator, withArgs);
                }
            }


            int CalculateSingleEntry(string fromUri, string toUri, WithTime<TransferMetric> calculator,
                WithArguments fileWriter)
            {
                var fromClean = fromUri.Replace("/", "_")
                    .Replace(":", "_")
                    .Replace(" ", "_");
                var toClean = toUri.Replace("/", "_")
                    .Replace(":", "_")
                    .Replace(" ", "_");

                var fileName = writeTo.Replace("{from}", fromClean)
                    .Replace("{to}", toClean);

                if (fileWriter.DoesAlreadyExist(fileName, fromUri, toUri))
                {
                    Console.Write("\rSKIP - already exists");
                    return 0;
                }

                var start = DateTime.Now;
                var isochrone = calculator.CalculateIsochroneFrom();
                List<Journey<TransferMetric>> journeys;
                if (isochrone == null || isochrone.Count == 1)
                {
                    journeys = null;
                }
                else
                {
                    journeys = calculator.CalculateAllJourneys();
                }

                var end = DateTime.Now;
                fileWriter.WriteToFile(fileName, journeys, (uint) (end - start).TotalMilliseconds);
                return journeys?.Count ?? 0;
            }
        }

        private class WithArguments
        {
            private const string _header =
                "from,from human, to, to human, departuretime,arrivaltime,totalTime,vehicles taken, vias (human readable, space seperated), vias (space seperated)";


            private readonly IStopsReader _reader;
            private readonly Stop _from;
            private readonly Stop _to;
            private readonly bool _summarize;

            public WithArguments(IStopsReader reader, string from, string to, bool summarize)
            {
                _reader = reader;
                _summarize = summarize;
                _from = GetStop(from);
                _to = GetStop(to);
            }

            private Dictionary<string, string[]> _cache = new Dictionary<string, string[]>();

            public bool DoesAlreadyExist(string fileName, string from, string to)
            {
                if (!_cache.ContainsKey(fileName))
                {
                    if (!File.Exists(fileName))
                    {
                        return false;
                    }

                    _cache[fileName] = File.ReadAllLines(fileName);
                }

                var text = _cache[fileName];
                foreach (var line in text)
                {
                    if (line.Contains(from) && line.Contains(to))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void WriteToFile(string fileName, List<Journey<TransferMetric>> js, uint timeNeededMs)
            {
                if (!File.Exists(fileName))
                {
                    File.WriteAllText(fileName, (_summarize ? _headerSummarized : _header) + "\n");
                }

                File.AppendAllText(fileName, GenerateOutputFor(js, timeNeededMs) + "\n");
            }

            private Stop GetStop(StopId id)
            {
                _reader.MoveTo(id);
                return new Stop(_reader);
            }

            private Stop GetStop(string id)
            {
                _reader.MoveTo(id);
                return new Stop(_reader);
            }


            private string GenerateOutputFor(List<Journey<TransferMetric>> js, uint timeNeededMs)
            {
                if (_summarize)
                {
                    return CreateJourneysSummary(js, timeNeededMs);
                }

                return string.Join("\n", js.Select(CreateSingleJourneyString));
            }

            private string CreateSingleJourneyString(Journey<TransferMetric> j)
            {
                var dep = GetStop(j.Root.Location);

                var arr = GetStop(j.Location);

                var vias = new List<string>();
                var viasHuman = new List<string>();

                foreach (var part in j.ToList())
                {
                    if (part.SpecialConnection)
                    {
                        var via = GetStop(part.Location);
                        vias.Add(via.GlobalId);
                        viasHuman.Add(via.GetName());
                    }
                }

                return $"{dep.GlobalId},{dep.GetName()},{arr.GlobalId},{arr.GetName()}," +
                       $"{j.Root.Time.FromUnixTime():s},{j.Time.FromUnixTime():s}," +
                       $"{j.Metric.NumberOfVehiclesTaken}," +
                       string.Join(" ", viasHuman) + "," +
                       string.Join(" ", vias);
            }

            private const string _headerSummarized =
                "from, from-name, to, to-name, distance in meters, " +
                "calculationTime (ms),journeysFound, " +
                "least time needed (s), least time journey departure time, " +
                "most time needed (s), most time journey departure time, " +
                "least vehicles needed (s), least vehicle journey departure time, " +
                "most vehicles needed (s), most vehicle journey departure time";

            private string CreateJourneysSummary(List<Journey<TransferMetric>> journeys, uint timeNeededMs)
            {
                var commonPart = $"{_from.GlobalId}, {_from.GetName()},{_to.GlobalId},{_to.GetName()}," +
                                 $"{DistanceEstimate.DistanceEstimateInMeter(_from.Latitude, _from.Longitude, _to.Latitude, _to.Longitude)}," +
                                 $"{timeNeededMs},";
                if (journeys == null || journeys.Count == 0)
                {
                    return commonPart + "0";
                }

                var ((bestPick, bestPickTime), (worstPick, worstPickTime), (bestTransfer, bestTransferCount), (
                        worstTransfer, worstTransferCount))
                    = PickJourneys(journeys);

                return
                    commonPart +
                    $"{journeys.Count}," +
                    $"{bestPickTime},{bestPick.DepartureTime().FromUnixTime():s}," +
                    $"{worstPickTime},{worstPick.DepartureTime().FromUnixTime():s}," +
                    $"{bestTransferCount},{bestTransfer.DepartureTime().FromUnixTime():s}," +
                    $"{worstTransferCount},{worstTransfer.DepartureTime().FromUnixTime():s}";
            }

            private static
                ((Journey<TransferMetric> bestPick, uint bestPickTime), (Journey<TransferMetric> worstPick, uint
                worstPickTime), (Journey<TransferMetric> bestTransfer, uint bestTransferCount), (Journey<TransferMetric>
                worstTransfer, uint worstTransferCount))
                PickJourneys(
                    IEnumerable<Journey<TransferMetric>> journeys)
            {
                uint worstPickTime = 0;
                var bestPickTime = uint.MaxValue;
                Journey<TransferMetric> worstPick = null;
                Journey<TransferMetric> bestPick = null;

                uint worstTransferCount = 0;
                var bestTransferCount = uint.MaxValue;
                Journey<TransferMetric> worstTransfer = null;
                Journey<TransferMetric> bestTransfer = null;


                foreach (var journey in journeys)
                {
                    var metric = journey.Metric;
                    var inVehicleTime = (uint) (metric.TravelTime - metric.WalkingTime);

                    if (worstPickTime < inVehicleTime)
                    {
                        worstPick = journey;
                        worstPickTime = inVehicleTime;
                    }

                    if (bestPickTime > inVehicleTime)
                    {
                        bestPickTime = inVehicleTime;
                        bestPick = journey;
                    }

                    if (worstTransferCount < metric.NumberOfVehiclesTaken)
                    {
                        worstTransferCount = metric.NumberOfVehiclesTaken;
                        worstTransfer = journey;
                    }

                    if (bestTransferCount > metric.NumberOfVehiclesTaken)
                    {
                        bestTransferCount = metric.NumberOfVehiclesTaken;
                        bestTransfer = journey;
                    }
                }

                return ((bestPick, bestPickTime), (worstPick, worstPickTime), (bestTransfer, bestTransferCount),
                    (worstTransfer, worstTransferCount));
            }
        }
    }
}