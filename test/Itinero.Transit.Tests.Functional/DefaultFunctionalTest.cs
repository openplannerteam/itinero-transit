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

        public bool ContainsLoop(Journey<T> journey)
        { 
            var seen = new HashSet<LocationId>();
            var curStop = journey.Location;

            while (journey != null)
            {
                if (!curStop.Equals(journey.Location))
                {
                    if (seen.Contains(journey.Location))
                    {
                        return true;
                    }

                    seen.Add(curStop);
                    curStop = journey.Location;
                }

                journey = journey.PreviousLink;
            }

            return false;   
        }

        public void NoLoops(Journey<T> journey, IStopsReader stops)
        {
            if (ContainsLoop(journey))
            {
                throw new Exception("Loop detected: "+journey.ToString(50, stops));
            }


        }
    }
}