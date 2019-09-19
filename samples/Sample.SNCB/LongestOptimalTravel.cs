using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Transit;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;

namespace Sample.SNCB
{
    /// <summary>
    /// The basic example which calculates routes over the Belgian Rail network
    /// </summary>
    public class LongestOptimalTravel
    {
        private const string outputDir = "output-filteroptimized";

        public static void Run()
        {
            Console.WriteLine("Calculating the longest optimal journey (for science) ");

            var date = new DateTime(2019, 09, 18, 10, 00, 00).ToUniversalTime()
                .Date; // DateTime.Now.ToUniversalTime().Date;
            var filePath = $"nmbs.{date:yyyy-MM-dd}.transitdb";

            var depTime = date.AddHours(1);
            var arrTime = depTime.AddHours(22);
            var transitDb = new TransitDb(0);

            if (File.Exists(filePath))
            {
                transitDb = TransitDb.ReadFrom(filePath, 0);
            }
            else
            {
                Console.WriteLine("Downloading data for today");
                transitDb.UseLinkedConnections(
                    "https://graph.irail.be/sncb/connections",
                    "https://irail.be/stations",
                    depTime, arrTime);


                transitDb.Latest.WriteTo(filePath);
            }

            // Create a traveller profile
            var profile = new DefaultProfile(0, 0);


            Console.WriteLine("Calculating journeys...");


            var cpuCount = 12;
            ThreadPool.SetMaxThreads(cpuCount, cpuCount);
            ThreadPool.SetMinThreads(cpuCount, cpuCount);


            for (int i = 0; i < cpuCount; i++)
            {
                var i1 = i;
                ThreadPool.QueueUserWorkItem(x =>
                    CalculateAll(i1, cpuCount, transitDb, profile, depTime, arrTime));
            }

            Thread.Sleep(TimeSpan.FromMinutes(60 * 5));

            Console.WriteLine("All done!");
        }


        private static void CalculateAll(
            int offset, int cpuCount,
            TransitDb transitDb, DefaultProfile profile, DateTime depTime, DateTime arrTime)
        {
            Console.WriteLine("Calculating with offset " + offset);
            var router = transitDb.SelectProfile(profile);
            var stopsReader = transitDb.Latest.StopsDb.GetReader();
            stopsReader.Reset();
            var stopsReader0 = transitDb.Latest.StopsDb.GetReader();
            var numberOfStops = 663;
            var path = "output.csv";
            var i = 0;

            var startOffset = 100;
            while (stopsReader.MoveNext())
            {
                if (i % cpuCount != offset)
                {
                    i++;
                    continue;
                }

                i++;


                if (i < startOffset)
                {
                    continue;
                }

                var pathI = outputDir + "/" + i + "_" + path;

                if (File.Exists(pathI))
                {
                    Console.WriteLine($"Skipping departure station {i} - already exists");
                    continue;
                }

                Console.WriteLine($"Calculating departure station {i}");
                var from = new Stop(stopsReader);
                CalculateAllForSingleFrom(i, 100,110,
                    pathI, stopsReader0, @from, router, depTime, arrTime, numberOfStops);
            }
        }


        private static void CalculateAllForSingleFrom(
            int i,
            int rangeStart, int rangeStop,
            string path, StopsDb.StopsDbReader stopsReader0,
            Stop @from,
            WithProfile<TransferMetric> router, DateTime depTime, DateTime arrTime, int numberOfStops)
        {
            uint calcSum = 0;
            var calcTimes = new List<uint>();

            var toWrite = new List<string>();
            toWrite.Add(
                "From, to, distance in meters, calcTimeNoFilter, calcTimeWithFilter," +
                " journeysFoundNoFilter, journeysFoundWithFilter, bestPickDeparture,bestPickTime, worstPickDeparture, worstPickTime");
            stopsReader0.Reset();
            var j = 0;

            while (stopsReader0.MoveNext())
            {
                j++;

                if (j < rangeStart)
                {
                    continue;
                }

                if (j > rangeStop)
                {
                    break;
                }
                
                var to = new Stop(stopsReader0);

                if (@from.Id.Equals(to.Id))
                {
                    continue;
                }


                var str = AnalyzeSingleFromTo(router, from, to, depTime, arrTime);

                toWrite.Add(str);

                if (j % 5 == 0)
                {
                    Console.WriteLine(
                        $"{i}/{numberOfStops}, {j}/{numberOfStops}, {@from.GlobalId} --> {to.GlobalId} ");
                }
            }

            File.AppendAllLines(path, toWrite);
            Console.WriteLine(
                $"Done: {from.GlobalId} Took {calcSum}ms, min:{calcTimes.Min()}, avg:{calcSum / calcTimes.Count}, max:{calcTimes.Max()}");
        }

        private static string AnalyzeSingleFromTo(WithProfile<TransferMetric> router,
            Stop @from, Stop to, DateTime depTime, DateTime arrTime)
        {
            var start = DateTime.Now;
            
            var calculator = router
                .SelectStops(from, to)
                .SelectTimeFrame(depTime, arrTime);
            calculator.CalculateIsochroneFrom();
            var journeys =
                calculator.CalculateAllJourneys(enableFiltering: false);
            var end = DateTime.Now;
            var timeNeededNoFilter = (uint) (end - start).TotalMilliseconds;

            start = DateTime.Now;
            calculator = router
                .SelectStops(from, to)
                .SelectTimeFrame(depTime, arrTime);
            calculator.CalculateIsochroneFrom();
            var journeysWithFiltering =
                calculator.CalculateAllJourneys(enableFiltering: true);
            end = DateTime.Now;
            var timeNeededWithFilter = (uint) (end - start).TotalMilliseconds;
            
            
            if (journeys == null)
            {
                return $"{@from.GlobalId},{to.GlobalId}," +
                       $"{DistanceEstimate.DistanceEstimateInMeter(@from.Latitude, @from.Longitude, to.Latitude, to.Longitude)},{timeNeededNoFilter},{timeNeededWithFilter}," +
                       $"{journeys?.Count ?? 0},{journeysWithFiltering?.Count ?? 0}";
            }
            var ((bestPick, bestTime), (worstPick, worstTime) ) = PickJourneys(journeys);

            if (journeys.Count != journeysWithFiltering.Count)
            {
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine($"From {from.GlobalId} to {to.GlobalId} yielded a different number of journeys");
                Console.WriteLine( $"{@from.GlobalId},{to.GlobalId}," +
                                   $"{DistanceEstimate.DistanceEstimateInMeter(@from.Latitude, @from.Longitude, to.Latitude, to.Longitude)}," +
                                   $"{timeNeededNoFilter},{timeNeededWithFilter}," +
                                   $"{journeys?.Count ?? 0},{journeysWithFiltering?.Count ?? 0}," +
                                   $"{bestPick.DepartureTime().FromUnixTime():s},{bestTime}," +
                                   $"{worstPick.DepartureTime().FromUnixTime():s},{worstTime}");
                AssertAreSame(journeys, journeysWithFiltering, calculator.StopsReader);
                Console.WriteLine(" - - - - - - - - - - - - - - -  - - - - - - - - -  - - - - - - - -  - - - - - - - - - - - - ");
                foreach (var j in journeys)
                {
                    Console.WriteLine(j.ToString(calculator, 100)+"\n");
                }
                throw new Exception("Crash");

            }
            

            // From, to, distance in meters, calcTime,bestPickDeparture,bestPickTime, worstPickDeparture, worstPickTime
            return
                $"{@from.GlobalId},{to.GlobalId}," +
                $"{DistanceEstimate.DistanceEstimateInMeter(@from.Latitude, @from.Longitude, to.Latitude, to.Longitude)}," +
                $"{timeNeededNoFilter},{timeNeededWithFilter}," +
                $"{journeys?.Count ?? 0},{journeysWithFiltering?.Count ?? 0}," +
                $"{bestPick.DepartureTime().FromUnixTime():s},{bestTime}," +
                $"{worstPick.DepartureTime().FromUnixTime():s},{worstTime}";
        }

        private static
            ((Journey<TransferMetric>, uint) bestPick, (Journey<TransferMetric>, uint) worstPick)
            PickJourneys(
                List<Journey<TransferMetric>> journeys)
        {
            uint worstPickTime = 0;
            var bestPickTime = uint.MaxValue;
            Journey<TransferMetric> worstPick = null;
            Journey<TransferMetric> bestPick = null;

            foreach (var journey in journeys)
            {
                var metr = journey.Metric;
                var inVehicleTime = (uint) (metr.TravelTime - metr.WalkingTime);

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
            }

            return ((bestPick, bestPickTime), (worstPick, worstPickTime));
        }
        
        
        // TODO REMOVE
        public static void AssertAreSame(ICollection<Journey<TransferMetric>> js, ICollection<Journey<TransferMetric>> bs,
            IStopsReader reader)
        {
            bool oneMissing = false;
            foreach (var a in js)
            {
                if (!bs.Contains(a))
                {
                    Console.WriteLine($"Missing journey: {a.ToString(100, reader)}");
                    oneMissing = true;
                }
            }

            int bi = 0;
            foreach (var b in bs)
            {
                if (!js.Contains(b))
                {
                   Console.WriteLine($"Missing journey {bi}: {b.ToString(100, reader)}");
                    oneMissing = true;
                }

                bi++;
            }

        }
    }
}