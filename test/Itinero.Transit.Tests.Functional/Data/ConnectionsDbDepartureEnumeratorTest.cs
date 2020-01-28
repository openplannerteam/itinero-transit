using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionsDbDepartureEnumeratorTest : FunctionalTestWithInput<(TransitDb, uint expectedNumberOfConnections)>
    {  public override string Name => "Departure Enumerator Test";

        protected override void Execute()
        {
            var latest = Input.Item1.Latest;

            // enumerate connections by departure time.
            var tt = 0;
            var forwardCount = (uint) 0;
            var connections = latest.Connections;
            var all = new List<string>();
            foreach (var c in connections)
            {
                tt += c.TravelTime;
                all.Add(c.GlobalId);
                forwardCount++;
            }



            // enumerate connections by departure time, but in reverse.
            tt = 0;
            forwardCount = 0;
            var departureEnumerator = latest.Connections.GetEnumeratorAt(latest.Connections.EarliestDate);
            var seenInForward = new HashSet<string>();

            while (departureEnumerator.MoveNext())
            {
                var cId = departureEnumerator.Current;
                var c = connections.Get(cId);
                if (seenInForward.Contains(c.GlobalId))
                {
                    throw new Exception($"Duplicate entry: {c.GlobalId}");
                }

                seenInForward.Add(c.GlobalId);

                tt += c.TravelTime;
                forwardCount++;
            }

            // enumerate connections by departure time, but in reverse.
            var backwardsCount = (uint) 0;
            departureEnumerator = latest.Connections.GetEnumeratorAt(latest.Connections.LatestDate + 1);
            var seenInBackwards = new HashSet<string>();
            while (departureEnumerator.MovePrevious())
            {
                var cId = departureEnumerator.Current;
                var c = connections.Get(cId);
                if (seenInBackwards.Contains(c.GlobalId))
                {
                    throw new Exception("Enumerated same connection twice: " + c.GlobalId);
                }

                seenInBackwards.Add(c.GlobalId);
                tt -= c.TravelTime;
                backwardsCount++;
            }


            var oneMissed = false;
            var i = 0;
            var cdb = latest.Connections;
            foreach (var cid in all)
            {
                if (!seenInForward.Contains(cid))
                {
                    oneMissed = true;
                    var c = cdb.Get(cid);
                    Information(
                        $"{i} The forwards enumerator did not contain {cid} (dep time {c.DepartureTime})");
                    i++;
                }
            }

            foreach (var cid in all)
            {
                if (!seenInBackwards.Contains(cid))
                {
                    oneMissed = true;
                    var c = latest.Connections.Get(cid);

                    Information($"The backwards enumerator did not contain {cid} (dep time {c.DepartureTime})");
                }
            }


            Equal(backwardsCount , forwardCount);
            Equal(backwardsCount , Input.expectedNumberOfConnections);
            Count(backwardsCount, all);
            True(!oneMissed);
            Equal(0, (uint) tt);
        }
    }
}