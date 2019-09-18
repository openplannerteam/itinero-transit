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
        public const string Brugge = "http://irail.be/stations/NMBS/008891009";
        public const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public const string Gent = "http://irail.be/stations/NMBS/008892007";


        public static void Run()
        {
            Console.WriteLine("Calculating the longest optimal journey (for science) ");

            var date = DateTime.Now.ToUniversalTime().Date;
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

            Thread.Sleep(TimeSpan.FromMinutes(60*5));

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
            var path = "AllCombinations.csv";
            var i = 0;
            while (stopsReader.MoveNext())
            {
                if (i % cpuCount != offset)
                {
                    i++;
                    continue;
                }

                i++;
                Console.WriteLine($"Calculating departure station {i}");
                var pathI = "output/"+i + "_" + path;

                if (File.Exists(pathI))
                {
                    continue;
                }

                var from = new Stop(stopsReader);
                CalculateAllForSingleFrom(i,pathI, stopsReader0, @from, router, depTime, arrTime, numberOfStops);
            }
        }


        private static void CalculateAllForSingleFrom(
            int i,
            string path, StopsDb.StopsDbReader stopsReader0,
            Stop @from,
            WithProfile<TransferMetric> router, DateTime depTime, DateTime arrTime, int numberOfStops)
        {
            uint calcSum = 0;
            var calcTimes = new List<uint>();

            var toWrite = new List<string>();
            toWrite.Add(
                "From, to, distance in meters, calcTime, journeysFound, bestPickDeparture,bestPickTime, worstPickDeparture, worstPickTime");
            stopsReader0.Reset();
            var j = 0;

            while (stopsReader0.MoveNext())
            {
                j++;

                var to = new Stop(stopsReader0);

                if (@from.Id.Equals(to.Id))
                {
                    continue;
                }

                var start = DateTime.Now;
                var calculator = router
                    .SelectStops(@from, to)
                    .SelectTimeFrame(depTime, arrTime);
                calculator.CalculateIsochroneFrom();
                var journeys =
                    calculator.CalculateAllJourneys();
                var end = DateTime.Now;
                var timeNeeded = (uint) (end - start).TotalMilliseconds;
                calcSum += timeNeeded;
                calcTimes.Add(timeNeeded);


                if (j % 25 == 0)
                {
                    Console.WriteLine(
                        $"{i}/{numberOfStops}, {j}/{numberOfStops}, {@from.GlobalId} --> {to.GlobalId} ");
                }

                if (journeys == null)
                {
                    toWrite.Add($"{@from.GlobalId},{to.GlobalId}," +
                                $"{DistanceEstimate.DistanceEstimateInMeter(@from.Latitude, @from.Longitude, to.Latitude, to.Longitude)},{timeNeeded},0"
                    );
                    continue;
                }


                var ((bestPick, bestTime), (worstPick, worstTime) ) = PickJourneys(journeys);

                // From, to, distance in meters, calcTime,bestPickDeparture,bestPickTime, worstPickDeparture, worstPickTime
                toWrite.Add(
                    $"{@from.GlobalId},{to.GlobalId}," +
                    $"{DistanceEstimate.DistanceEstimateInMeter(@from.Latitude, @from.Longitude, to.Latitude, to.Longitude)},{timeNeeded}," +
                    $"{journeys.Count}" +
                    $"{bestPick.DepartureTime().FromUnixTime():s},{bestTime}," +
                    $"{worstPick.DepartureTime().FromUnixTime():s},{worstTime}"
                );
            }
            File.AppendAllLines(path,toWrite );
            Console.WriteLine(
                $"Done: {from.GlobalId} Took {calcSum}ms, min:{calcTimes.Min()}, avg:{calcSum / calcTimes.Count}, max:{calcTimes.Max()}");
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
    }
}