using System;
using Itinero.Transit;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.LC;

namespace Sample.SNCB
{
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            // create an empty transit db
            // Note that every transitDB has an unique identifier, in this case '0'.
            var transitDb = new TransitDb(0);
            Console.WriteLine("Loading connections...");
            
            // specify where to get data from, in this case linked connections for the Belgian rail operator.
            transitDb.UseLinkedConnections("https://graph.irail.be/sncb/connections",
                "https://irail.be/stations", 
                DateTime.Now, DateTime.Now.AddHours(5));

            // get a snapshot of the db to use.
            var snapshot = transitDb.Latest;

            // look up departure/arrival stops.
            var departureStop = snapshot.FindClosestStop(new Stop(4.9376678466796875, 51.322734170650484));
            var arrivalStop = snapshot.FindClosestStop(new Stop(4.715280532836914, 50.88132251839807));
            // Create a traveller profile
            var profile = new DefaultProfile();


            Console.WriteLine("Calculating journeys...");

            var router = snapshot
                .SelectProfile(profile)
                .SelectStops(departureStop, arrivalStop)
                .SelectTimeFrame(DateTime.Now, DateTime.Now.AddHours(3));
            var journeys = router.CalculateAllJourneys();
            if (journeys == null || journeys.Count == 0)
            {
                Console.WriteLine("No journeys found.");
            }
            else

            {
                foreach (var journey in journeys)
                {
                    Console.WriteLine(journey.ToString(router));
                }
            }
        }
    }
}