using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journey.Metric
{
    /// <inheritdoc />
    /// <summary>
    /// A simple Journey Comparer, which walks along two journeys and takes the difference in station importance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MaximizeStations<T> : Comparer<Journey<T>> where T : IJourneyMetric<T>
    {
        private readonly Dictionary<LocationId, uint> _importances;

        public MaximizeStations(Dictionary<LocationId, uint> importances)
        {
            _importances = importances;
        }


        public override int Compare(Journey<T> x, Journey<T> y)
        {
            var sum = 0;
            if (x == null || y == null)
            {
                throw new NullReferenceException();
            }

            if (x.PreviousLink != null && y.PreviousLink != null)
            {
                sum += Compare(x.PreviousLink, y.PreviousLink);
            }

            _importances.TryGetValue(x.Location, out var xL);
            _importances.TryGetValue(y.Location, out var yL);


            sum += (int) yL - (int) xL;
            return sum;
        }
    }
}