using System;
using System.Collections.Generic;
using GeoAPI.Geometries;
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

            var seen = new HashSet<LocationId>();
            
            while (journey != null)
            {
                if (!curStop.Equals(journey.Location))
                {
                    if (seen.Contains(journey.Location))
                    {
                        stops.MoveTo(journey.Location);
                        throw new Exception($"Already been to this stop: {stops.GlobalId}.\n Journey was "+fullJourney.ToString(100, stops));
                    }

                    seen.Add(curStop);
                    curStop = journey.Location;
                }

                journey = journey.PreviousLink;
            }
        }
    }
}