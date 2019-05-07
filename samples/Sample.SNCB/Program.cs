using System;
using Itinero.Transit;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;

namespace Sample.SNCB
{
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            // create an empty transit db and specify where to get data from, in this case linked connections.
            var transitDb = new TransitDb();
            Console.WriteLine("Loading connections...");
            transitDb.UseLinkedConnections("https://graph.irail.be/sncb/connections",
                "https://irail.be/stations", DateTime.Now, DateTime.Now.AddHours(5));

            // get a snapshot of the db to use.
            var snapshot = transitDb.Latest;

            // look up departure/arrival stops.
            var departureStop = snapshot.FindClosestStop(4.9376678466796875, 51.322734170650484);
            var arrivalStop = snapshot.FindClosestStop(4.715280532836914, 50.88132251839807);
            // Create a traveller profile
            var profile = new DefaultProfile();


            Console.WriteLine("Calculating journeys...");

            var journeys = snapshot
                .SelectProfile(profile)
                .SelectStops(departureStop, arrivalStop)
                .SelectTimeFrame(DateTime.Now, DateTime.Now.AddHours(3))
                .AllJourneys();
            if (journeys == null || journeys.Count == 0)
            {
                Console.WriteLine("No journeys found.");
            }
            else

            {
                foreach (var journey in journeys)
                {
                    Console.WriteLine(journey.ToString(snapshot));
                }
            }
        }
    }
}