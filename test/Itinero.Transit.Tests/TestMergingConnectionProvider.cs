using System.Collections.Generic;
using System.Linq;
using Itinero.Transit;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class TestMergingConnectionProvider
    {
        private readonly ITestOutputHelper _output;

        public TestMergingConnectionProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
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
            IConnection prev = null;
            var graph = new List<IConnection>(tt.Connections());
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
            Assert.Equal(ResourcesTest.TestMoment(4, 00), deLijnTt.StartTime());

            Assert.Equal(ResourcesTest.TestMoment(4, 05), deLijnTt.EndTime());

            Assert.Equal(ResourcesTest.TestMoment(3, 51), sncbTt.StartTime());
            Assert.Equal(ResourcesTest.TestMoment(4, 05), sncbTt.EndTime());

            Assert.Equal(538, graph.Count);
            Assert.Equal(111, sncbTt.Connections().Count());
            Assert.Equal(497, deLijnTt.Connections().Count());

            foreach (var conn in graph)
            {
                Assert.True(conn.DepartureTime() >= tt.StartTime());
                Assert.True(conn.DepartureTime() <= tt.EndTime());
            }

            var graphR = new List<IConnection>(tt.ConnectionsReversed());

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


            var graph = new List<IConnection>(tt.Connections());
            var graphR = new List<IConnection>(tt.ConnectionsReversed());

            Assert.Equal(graph.Count(), graphR.Count);

            for (var i = 0; i < graph.Count(); i++)
            {
                Assert.Equal(graph[i].DepartureTime(), graphR[graph.Count - 1 - i].DepartureTime());
            }
        }
    }
}