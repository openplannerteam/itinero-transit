using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Reminiscence.Collections;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :
        DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6
            
            var pcs = new ProfiledConnectionScan<TransferMetric>(input.GetScanSettings());
            var journeys= pcs.CalculateJourneys();
            
            // verify result.
            NotNull(journeys);
            var withLoop = new List<Journey<TransferMetric>>();
            foreach (var journey in journeys)
            {
                if (ContainsLoop(journey))
                {
                    withLoop.Add(journey);
                }
            }

            True(journeys.Any());

            Information($"Found {journeys.Count} profiles");
            
            
            // Verify properties on Pareto frontiers

           var stationJourneys= pcs.StationJourneys();
           foreach (var kv in stationJourneys)
           {
               var frontier = kv.Value.Frontier;

               var lastDep = frontier[0];
               foreach (var journey in frontier)
               {
                   if (lastDep.Time < journey.Time)
                   {
                       throw new Exception("Not sorted. A journey departs earlier then its predecessor");
                   }

                   lastDep = journey;
               }
               
           }
            return true;
        }
    }
}