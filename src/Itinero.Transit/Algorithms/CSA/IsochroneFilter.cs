using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
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
            var stop = _isForward ? c.DepartureStop : c.ArrivalStop;

            _isochrone.TryGetValue(stop, out var journey);
            if (journey == null)
            {
                // The isochrone indicates that this stop can never be reached within the given time
                return false;
            }

            var time = journey.Time;

            if (_isForward)
            {
                // Is the moment we can realistically arrive at the station before this connection?
                // If not, it is no use to take the train    
                return time <= c.DepartureTime;
            }
            // ReSharper disable once RedundantIfElseBlock
            else
            {
                return time >= c.ArrivalTime;
            }
        }

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            if (_earliestValidDate > depTime
                || _latestValidDate < arrTime)
            {
                throw new ArgumentException(
                    "The requesting algorithm requests connections outside of the valid range of this algorithm");
            }
        }
    }
}