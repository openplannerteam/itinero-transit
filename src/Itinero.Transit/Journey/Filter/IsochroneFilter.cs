using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.Journey.Filter
{
    internal static class MaskFilterConstructors
    {
        public static IsochroneFilter<T> AsFilter<T>(this EarliestConnectionScan<T> eas) where T : IJourneyMetric<T>
        {
            if (eas.ScanEndTime <= 0)
            {
                throw new ArgumentException("Trying to use an EAS as filter, but the EAS did not run yet");
            }

            return new IsochroneFilter<T>(eas.Isochrone(), true,
                eas.ScanBeginTime,
                eas.ScanEndTime);
        }

        public static IsochroneFilter<T> AsFilter<T>(this LatestConnectionScan<T> las) where T : IJourneyMetric<T>
        {
            if (las.ScanBeginTime == ulong.MaxValue)
            {
                throw new ArgumentException("Trying to use an LAS as filter, but the LAS did not run yet");
            }

            return new IsochroneFilter<T>(las.Isochrone(), false,
                las.ScanBeginTime,
                las.ScanEndTime);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// The Msk-Filter takes the isochrone line of EAS/LAS and decides if a connection is useful to take based on this.
    /// </summary>
    internal class IsochroneFilter<T> : IConnectionFilter
        where T : IJourneyMetric<T>
    {
        private readonly IReadOnlyDictionary<LocationId, Journey<T>> _isochrone;
        private readonly bool _isForward;

        private readonly ulong _earliestValidDate, _latestValidDate;

        public IsochroneFilter(IReadOnlyDictionary<LocationId, Journey<T>> isochrone, bool isForward,
            ulong earliestValidDate, ulong latestValidDate)
        {
            _isochrone = isochrone;
            _isForward = isForward;
            _earliestValidDate = earliestValidDate;
            _latestValidDate = latestValidDate;
        }


        public bool CanBeTaken(IConnection c)
        {
            if (_isForward)
            {
                // The normal, forward case

                // Can we take the train here at c.DepartureTime?
                // Only if we could already be here according to the isochrone
                _isochrone.TryGetValue(c.DepartureStop, out var journey);
                if (journey == null)
                {
                    // The isochrone indicates that this stop can never be reached within the given time
                    return false;
                }

                // At what time does this journey arrive here?
                return journey.ArrivalTime() <= c.DepartureTime;
            }
            else
            {
                // The reverse logic. The isochrone descripts when we should have departed at a certain location
                // to still be able to arrive at the given timeframe

                // In other words: is the arrival in 'ArrivalStop' at 'ArrivalTime'
                // before the departure the journey towards the final destination
                _isochrone.TryGetValue(c.ArrivalStop, out var journey);

                if (journey == null)
                {
                    return false;
                }

                return journey.Root.DepartureTime() >= c.ArrivalTime;
            }
        }


        public bool ValidWindow(ulong depTime, ulong arrTime)
        {
            return !(_earliestValidDate > depTime
                     || _latestValidDate < arrTime);
        }

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            if (!ValidWindow(depTime, arrTime))
            {
                throw new ArgumentException(
                    "The requesting algorithm requests connections outside of the valid range of this algorithm");
            }
        }
    }
}