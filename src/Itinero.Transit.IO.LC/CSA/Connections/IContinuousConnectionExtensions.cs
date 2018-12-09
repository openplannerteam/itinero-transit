//using System;
//
//namespace Itinero.Transit.IO.LC.CSA.Connections
//{
//    internal static class ContinuousConnectionExtensions
//    {
//        // Getting close to java names!
//
//        public static IContinuousConnection MoveArrivalTime(this IContinuousConnection c, DateTime arrivalTime)
//        {
//            var diff = c.ArrivalTime() - c.DepartureTime();
//            return c.MoveDepartureTime(arrivalTime - diff);
//        }
//    }
//}