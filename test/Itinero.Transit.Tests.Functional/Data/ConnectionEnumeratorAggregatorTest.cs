using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionEnumeratorAggregatorTest : FunctionalTest<bool, (IEnumerable<TransitDb> dbs, DateTime date)>
    {
        protected override bool Execute((IEnumerable<TransitDb> dbs, DateTime date) input)
        {

            var reader = ConnectionEnumeratorAggregator.CreateFrom(input.dbs.Select(a => a.Latest));
            reader.MovePrevious(input.date.Date.AddHours(3));
            //while (reader.DepartureTime.FromUnixTime() < input.date.Date.AddHours(1))
            var alreadySeen = new HashSet<string>();
            var timeout = 10;
            while(reader.MovePrevious())
            {
                Information($"{reader.DepartureTime.FromUnixTime():s}");
                var id = reader.GlobalId;
                Information(id);
                if (alreadySeen.Contains(id))
                {
                    if (timeout < 0)
                    {
                        Information("Already seen");
                    throw new Exception("Already seen");
                    }

                    timeout--;
                }
                alreadySeen.Add(id);
                
            }

            return true;

        }
    }
}