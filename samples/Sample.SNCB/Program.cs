using System;
using System.Linq;
using Itinero.Transit;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.IO.LC;
using Itinero.Transit.Journeys;

namespace Sample.SNCB
{
    class Program
    {
        static void Main(string[] args)
        {
            // create an empty transit db.
            var transitDb = new TransitDb();
            
            // specify where to get data from, in this case linked connections.
            Console.WriteLine("Loading connections...");
            transitDb.UseLinkedConnections("https://graph.irail.be/sncb/connections",
                "https://irail.be/stations", DateTime.Now, DateTime.Now.AddHours(5));
            
            // get a snapshot of the DB to use.
            var snapshot = transitDb.Latest;

            // define a profile, here total travel time and number of transfers are minimized
            // TODO: make this simpler or even default.
            var p = new Profile<TransferStats>(
                    // The time it takes to transfer from one train to another (and the underlying algorithm, in this case: always the same time)
                    new InternalTransferGenerator(180 /*seconds*/), 

                    // The intermodal stop algorithm. Note that a transitDb is used to search stop location
                    new CrowsFlightTransferGenerator(snapshot, maxDistance: 500 /*meter*/,  speed: 1.4f /*meter/second*/),

                    // The object that can create a metric
                    TransferStats.Factory,

                    // The comparison between routes. _This comparison should check if two journeys are covering each other, as seen in core concepts!_
                    TransferStats.ProfileTransferCompare
                );

            // look up departure/arrival stops.
            var departureStop = snapshot.FindClosestStop(4.9376678466796875,51.322734170650484);
            var arrivalStop = snapshot.FindClosestStop(4.715280532836914,50.88132251839807);

            // calculate journeys.
            Console.WriteLine("Calculating journeys...");
            var journeys = snapshot.CalculateJourneys(p, departureStop.Id, arrivalStop.Id, 
                DateTime.Now)?.ToList();
            if (journeys == null || 
                journeys.Count == 0)
            {
                Console.WriteLine("No journeys found.");
            }
            else
            {
                foreach (var journey in journeys)
                {
                    Console.WriteLine(journey.ToString(snapshot.StopsDb));
                }
            }
        }
    }
}