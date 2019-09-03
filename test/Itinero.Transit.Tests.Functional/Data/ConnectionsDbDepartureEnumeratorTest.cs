using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionsDbDepartureEnumeratorTest : FunctionalTestWithInput<(TransitDb, uint expectedNumberOfConnections)>
    {
        protected override void Execute()
        {
            var latest = Input.Item1.Latest;

            // enumerate connections by departure time.
            var tt = 0;
            var forwardCount = 0;
            var enumerator = latest.ConnectionsDb;
            var index = enumerator.First().Value;
            var all = new List<uint>();
            do
            {
                var c = enumerator.Get(index);
                tt += c.TravelTime;
                all.Add(index.InternalId);
                forwardCount++;
            } while (enumerator.HasNext(index, out index));

            var departureEnumerator = latest.ConnectionsDb.GetDepartureEnumerator();


            // enumerate connections by departure time, but in reverse.
            tt = 0;
            forwardCount = 0;
            departureEnumerator.MoveTo(latest.ConnectionsDb.EarliestDate);
            var seenInForward = new HashSet<uint>();

            while (departureEnumerator.HasNext())
            {
                var c = departureEnumerator.Current();
                if (seenInForward.Contains(c.Id.InternalId))
                {
                    throw new Exception($"Duplicate entry: {c.Id}");
                }

                seenInForward.Add(c.Id.InternalId);

                tt += c.TravelTime;
                forwardCount++;
            }

            // enumerate connections by departure time, but in reverse.
            var backwardsCount = 0;
            departureEnumerator.MoveTo(latest.ConnectionsDb.LatestDate);
            var seenInBackwards = new HashSet<uint>();
            while (departureEnumerator.HasPrevious())
            {
                var c = departureEnumerator.Current();
                if (seenInBackwards.Contains(c.Id.InternalId))
                {
                    throw new Exception("Enumerated same connection twice: " + c.GlobalId);
                }

                seenInBackwards.Add(c.Id.InternalId);
                tt -= c.TravelTime;
                backwardsCount++;
            }


            var oneMissed = false;
            var i = 0;
            var cdb = latest.ConnectionsDb;
            foreach (var cid in all)
            {
                if (!seenInForward.Contains(cid))
                {
                    oneMissed = true;
                    var c = cdb.Get(
                        new ConnectionId(0, cid));
                    Information(
                        $"{i} The forwards enumerator did not contain {cid} (dep time {c.DepartureTime} ({cdb.WindowFor(c.DepartureTime)})");
                    i++;
                }
            }

            foreach (var cid in all)
            {
                if (!seenInBackwards.Contains(cid))
                {
                    oneMissed = true;
                    var c = latest.ConnectionsDb.Get(new ConnectionId(0, cid));

                    Information($"The backwards enumerator did not contain {cid} (dep time {c.DepartureTime})");
                }
            }


            True(backwardsCount == forwardCount);
            True(backwardsCount == Input.expectedNumberOfConnections);
            True(all.Count == backwardsCount);
            True(!oneMissed);
            True(tt == 0);
        }
    }
}