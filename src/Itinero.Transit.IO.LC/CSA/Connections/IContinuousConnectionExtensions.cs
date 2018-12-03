using System;
namespace Itinero.IO.LC
{
    public static class ContinuousConnectionExtensions
    {
        // Getting close to java names!

        public static IContinuousConnection MoveArrivalTime(this IContinuousConnection c, DateTime arrivalTime)
        {
            var diff = c.ArrivalTime() - c.DepartureTime();
            return c.MoveDepartureTime(arrivalTime - diff);
        }
    }
}