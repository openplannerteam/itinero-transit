using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class IsochroneTest : DefaultFunctionalTest
    {
        
        
        
        protected override bool Execute(
            (TransitDb transitDb,
                string departureStopId, string arrivalStopId, DateTime departureTime, DateTime arrivalTime)
                input)
        {

            var reader = input.transitDb.Latest.StopsDb.GetReader();
            reader.MoveTo(input.arrivalStopId);
            var checkId = reader.Id;
            
            var latest = input.transitDb.Latest;
            var profile = new Profile<TransferStats>(latest,
                new InternalTransferGenerator(1),
                null,
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var found = profile.Isochrone(input.departureStopId, input.departureTime, input.arrivalTime);

            Assert.True(found.Count() > 100);
            Assert.Contains(checkId, found);
            var timeNeeded = found[checkId].Time.FromUnixTime() - input.departureTime;
            Console.WriteLine($"Found route in the forward isochrone line between Bruges and Ghent: \n{found[checkId].ToString(reader)}");


            found = null;
            found = profile.IsochroneLatestArrival(input.arrivalStopId, input.departureTime, input.arrivalTime);

            Assert.True(found.Count() > 100);
            Assert.Contains(checkId, found);
            Console.WriteLine($"Found route in the backward isochrone line between Bruges and Ghent: \n{found[checkId].ToString(reader)}");
            timeNeeded = found[checkId].Time.FromUnixTime() - input.arrivalTime;
            
            
            
            return true;
        }
    }
}