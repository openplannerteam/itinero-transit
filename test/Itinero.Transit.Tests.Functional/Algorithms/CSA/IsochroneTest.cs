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

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(1),
                null,
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);

            var tbd = input.transitDb.Latest.SelectProfile(profile)
                .SelectStops(input.departureStopId, input.arrivalStopId)
                .SelectTimeFrame(input.departureTime, input.arrivalTime);
            
            var found = tbd
                .IsochroneFrom();
                

            True(found.Count() > 10);
          
            found = tbd.IsochroneTo();
            
            True(found.Count() > 10);
            
            return true;
        }
    }
}