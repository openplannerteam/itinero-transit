using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional
{
    public abstract class DefaultFunctionalTest<T> :
        FunctionalTest<bool, WithTime<T>> where T : IJourneyMetric<T>
    {
        public void NoLoops(Journey<T> journey, WithTime<TransferMetric> info)
        {
            NoLoops(journey, info.StopsReader);
        }

        public void NoLoops(Journey<T> journey, IStopsReader stops)
        {
            var fullJourney = journey;

            var curStop = journey.Location;

            return;
            while (journey != null)
            {
                if (!curStop.Equals(journey.Location))
                {
                    curStop = journey.Location;
                    if (curStop.Equals(fullJourney.Location))
                    {
                        // We already were at the destination, but are there again?
                        throw new Exception("WUT? "+fullJourney.ToString(50, stops));
                    }
                }

                journey = journey.PreviousLink;
            }
        }
    }
}