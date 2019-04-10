using System;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class IsochroneTest : DefaultFunctionalTest
    {

        public static IsochroneTest Default = new IsochroneTest();

        protected override bool Execute(
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime departureTime, DateTime
                arrivalTime)
                input)
        {
            var reader = input.transitDb.Latest.StopsDb.GetReader();
            reader.MoveTo(input.arrivalStopId);

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(1),
                null,
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var tbd = input.transitDb.Latest;
            var found = tbd.CalculateIsochrone(profile, input.departureStopId, input.departureTime, input.arrivalTime);

            True(found.Count() > 10);
          
            found = tbd.CalculateIsochroneLatestArrival(profile, input.departureStopId, input.departureTime, input.arrivalTime);

            True(found.Count() > 10);
            
            return true;
        }
    }
}