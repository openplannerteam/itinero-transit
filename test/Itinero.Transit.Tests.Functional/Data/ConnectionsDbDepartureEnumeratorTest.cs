using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionsDbDepartureEnumeratorTest : FunctionalTest<int, TransitDb>
    {
        protected override int Execute(TransitDb input)
        {
            var latest = input.Latest;

            // enumerate connections by departure time.
            var tt = 0;
            var forwardCount = 0;
            var enumerator = latest.ConnectionsDb.GetReader();
            var index = enumerator.First().Value;
            var all = new List<uint>();
            while (enumerator.HasNext(index, out index))
            {
                var c = enumerator.Get(index);
                tt += c.TravelTime;
                all.Add(index.InternalId);
                forwardCount++;
            }

            Information($"Enumerated {forwardCount} connections!");

            var departureEnumerator = latest.ConnectionsDb.GetDepartureEnumerator();


            Information("Starting backwards enumeration");
            // enumerate connections by departure time, but in reverse.
            var backwardsCount = 0;
            departureEnumerator.MoveTo(latest.ConnectionsDb.LatestDate);
            var seenInBackwards = new List<uint>();
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


            Information("Starting Forwards enumeration");
            // enumerate connections by departure time, but in reverse.
            tt = 0;
            forwardCount = 0;
            departureEnumerator.MoveTo(latest.ConnectionsDb.EarliestDate);
            var seenInForward = new List<uint>();

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
                    Information($"{i} The forwards enumerator did not contain {cid} (dep time {c.DepartureTime} ({cdb.WindowFor(c.DepartureTime)})");
                    i++;
                }
            }

            foreach (var cid in all)
            {
                if (!seenInBackwards.Select(x => x == cid).Any())
                {
                    oneMissed = true;
                    var c = latest.ConnectionsDb.Get(new ConnectionId(0, cid));

                    Information($"The backwards enumerator did not contain {cid} (dep time {c.DepartureTime})");
                }
            }

            True(!oneMissed);

            Information($"Enumerated forward, {tt}");


            True(backwardsCount == forwardCount);
            True(tt == 0);
            Information($"Enumerated forward, {tt}");

            return forwardCount;
        }
    }
}