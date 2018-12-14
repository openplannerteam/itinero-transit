using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class CountArrivingConnections : 
        FunctionalTest<uint, (ConnectionsDb, (uint, uint))>
    {
        
        public static readonly CountArrivingConnections Default = new CountArrivingConnections();
        
        protected override uint Execute((ConnectionsDb, (uint, uint)) input)
        {
            var count = (uint) 0;
            var enumerator = input.Item1.GetDepartureEnumerator();
            while (enumerator.MoveNext())
            {
                if (Equals(enumerator.ArrivalStop, input.Item2))
                {
                    count++;
                }
            }

            Information($"Counted {count} connections arriving at the requested location {input.Item2}");
            return count;
        }

    }
}