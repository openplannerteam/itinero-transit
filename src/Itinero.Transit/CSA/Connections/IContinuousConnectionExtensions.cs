using System;

namespace Itinero.Transit
{
    public static class IContinuousConnectionExtensions
    {
        // Getting close to java names!

        public static IContinuousConnection MoveArrivalTime(this IContinuousConnection c, DateTime arrivalTime)
        {
            var diff = c.ArrivalTime() - c.DepartureTime();
            return c.MoveDepartureTime(arrivalTime - diff);
        }
    }
}