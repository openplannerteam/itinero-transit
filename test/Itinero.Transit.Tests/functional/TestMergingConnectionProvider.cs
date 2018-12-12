using System.Collections.Generic;
using System.Linq;
using Itinero.IO.LC;
using Itinero.Transit;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.Connections;
using Itinero.Transit.IO.LC.CSA.Utils;
using Itinero.Transit.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.IO.LC.Tests
{
    public class TestMergingConnectionProvider : SuperTest
    {
        public TestMergingConnectionProvider(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestMerging()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);
            var sncb = Belgium.Sncb(st);
            var merged = new ConnectionProviderMerger(new List<IConnectionsProvider>
            {
                deLijn,
                sncb
            });

            // This moment (4AM) gives a neat mix of timetables:
            // Few trains, few buses so that the timetable of the buses are more then one minute long
            var moment = ResourcesTest.TestMoment(4, 00);


            var tt = merged.GetTimeTable(moment);
            LinkedConnection prev = null;
            var graph = new List<LinkedConnection>(tt.Connections());
            var seen = new HashSet<string>();
            foreach (var conn in graph)
            {
                Log($"TT1 {conn.DepartureTime():HH:mm} {conn.Id()}");
                Assert.True(prev == null || prev.DepartureTime() <= conn.DepartureTime());
                prev = conn;

                Assert.False(seen.Contains(conn.Id().ToString()));
                seen.Add(conn.Id().ToString());
            }


            var tt2 = merged.GetTimeTable(tt.NextTable());
            foreach (var conn in tt2.Connections())
            {
                Log($"TT2 {conn.DepartureTime():HH:mm} {conn.Id()}");
                Assert.True(prev == null || prev.DepartureTime() <= conn.DepartureTime());
                prev = conn;

                Assert.False(seen.Contains(conn.Id().ToString()));
                seen.Add(conn.Id().ToString());
            }

            var sncbTt = sncb.GetTimeTable(moment);
            Log(
                $"NMBS Table: {sncbTt.StartTime():HH:mm} --> {sncbTt.EndTime():HH:mm}, {sncbTt.Connections().Count()} entries");
            var deLijnTt = deLijn.GetTimeTable(moment);
            Log(
                $"De Lijn Table: {deLijnTt.StartTime():HH:mm} --> {deLijnTt.EndTime():HH:mm}, {deLijnTt.Connections().Count()} entries");

            Log(
                $"Synthetic table with {graph.Count} entries,  starting at {tt.StartTime()} till {tt.EndTime()}");
            Assert.Equal(ResourcesTest.TestMoment(3, 53), deLijnTt.StartTime());

            Assert.Equal(ResourcesTest.TestMoment(4, 05), deLijnTt.EndTime());

            Assert.True(ResourcesTest.TestMoment(3, 50) < sncbTt.StartTime());
            Assert.True(ResourcesTest.TestMoment(4, 10) > sncbTt.EndTime());

            Assert.True(518 < graph.Count);
            Assert.True(100 < sncbTt.Connections().Count());
            Assert.True(600 < deLijnTt.Connections().Count());

            foreach (var conn in graph)
            {
                Assert.True(conn.DepartureTime() >= tt.StartTime());
                Assert.True(conn.DepartureTime() <= tt.EndTime());
            }

            var graphR = new List<LinkedConnection>(tt.ConnectionsReversed());

            Assert.Equal(graph.Count(), graphR.Count());

            for (var i = 0; i < graph.Count(); i++)
            {
                Assert.Equal(graph[i].DepartureTime(), graphR[graph.Count - 1 - i].DepartureTime());
            }
        }

        [Fact]
        public void TestReverseMergedTable()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.WestVlaanderen(st, null);
            var sncb = Belgium.Sncb(st);
            var merged = new ConnectionProviderMerger(new List<IConnectionsProvider>
            {
                deLijn,
                sncb
            });

            // This moment (4AM) gives a neat mix of timetables:
            // Few trains, few buses so that the timetable of the buses are more then one minute long
            var moment = ResourcesTest.TestMoment(4, 00);


            var tt = merged.GetTimeTable(moment);


            var graph = new List<LinkedConnection>(tt.Connections());
            var graphR = new List<LinkedConnection>(tt.ConnectionsReversed());

            Assert.Equal(graph.Count(), graphR.Count);

            for (var i = 0; i < graph.Count(); i++)
            {
                Assert.Equal(graph[i].DepartureTime(), graphR[graph.Count - 1 - i].DepartureTime());
            }
        }
    }
}