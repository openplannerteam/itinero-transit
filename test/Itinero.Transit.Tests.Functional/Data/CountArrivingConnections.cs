using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class CountArrivingConnections :
        FunctionalTest<uint, (ConnectionsDb, StopId)>
    {
        public static readonly CountArrivingConnections Default = new CountArrivingConnections();

        protected override uint Execute((ConnectionsDb, StopId) input)
        {
            var count = (uint) 0;
            var enumerator = input.Item1;
            var index = enumerator.First().Value;
            while (enumerator.HasNext(index, out index))
            {
                var c = enumerator.Get(index);
                if (Equals(c.ArrivalStop, input.Item2))
                {
                    count++;
                }
            }

            Information($"Counted {count} connections arriving at the requested location {input.Item2}");
            return count;
        }
    }
}