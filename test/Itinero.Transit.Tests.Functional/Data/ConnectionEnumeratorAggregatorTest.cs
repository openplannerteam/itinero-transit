using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionEnumeratorAggregatorTest : FunctionalTest<bool, (IEnumerable<TransitDb> dbs, DateTime date)>
    {
        protected override bool Execute((IEnumerable<TransitDb> dbs, DateTime date) input)
        {

            var reader = ConnectionEnumeratorAggregator.CreateFrom(input.dbs.Select(a => a.Latest.ConnectionsDb.GetDepartureEnumerator()));
            reader.MoveTo(input.date.Date.AddHours(3).ToUnixTime());
            //while (reader.DepartureTime.FromUnixTime() < input.date.Date.AddHours(1))
            var alreadySeen = new HashSet<string>();
            var timeout = 10;
            var c = new Connection();
            while(reader.HasPrevious())
            {
                reader.Current(c);
                Information($"{c.DepartureTime.FromUnixTime():s}");
                var id = c.GlobalId;
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