using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class IsochroneTest : FunctionalTest<bool, (TransitDb transitDb, string stopId, string checkIsIncluded, DateTime departureTime,
        DateTime
        arrivalTime)>
    {
        protected override bool Execute(
            (TransitDb transitDb, string stopId, string checkIsIncluded, DateTime departureTime, DateTime
                arrivalTime)
                input)
        {

            var reader = input.transitDb.Latest.StopsDb.GetReader();
            reader.MoveTo(input.checkIsIncluded);
            var checkId = reader.Id;
            
            var latest = input.transitDb.Latest;
            var profile = new Profile<TransferStats>(latest,
                new InternalTransferGenerator(1),
                null,
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var tbd = input.transitDb;
            var found = tbd.Isochrone(profile, input.stopId, input.departureTime, input.arrivalTime);

            Assert.True(found.Count() > 100);
            Assert.Contains(checkId, found);
            var timeNeeded = found[checkId].Time.FromUnixTime() - input.departureTime;
            Console.WriteLine($"Found route in the forward isochrone line between Bruges and Ghent: \n{found[checkId].ToString(reader)}");
            Assert.True(timeNeeded < TimeSpan.FromMinutes(45));


            found = tbd.IsochroneLatestArrival(profile, input.stopId, input.departureTime, input.arrivalTime);

            Assert.True(found.Count() > 100);
            Assert.Contains(checkId, found);
            Console.WriteLine($"Found route in the backward isochrone line between Bruges and Ghent: \n{found[checkId].ToString(reader)}");
            timeNeeded = found[checkId].Time.FromUnixTime() - input.arrivalTime;
            Assert.True(timeNeeded < TimeSpan.FromMinutes(45));
            
            
            
            return true;
        }
    }
}