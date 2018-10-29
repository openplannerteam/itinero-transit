using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
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
            var loader = new Downloader();
            var deLijn = DeLijn.Profile(loader,
                new LocalStorage("cache/delijn"), "belgium.routerdb");
            var sncb = Sncb.Profile(loader, new LocalStorage("cache/sncb"), "belgium.routerdb");
            var merged = new ConnectionProviderMerger(new List<IConnectionsProvider>()
            {
                deLijn,
                sncb
            });

            // This moment (4AM) gives a neat mix of timetables:
            // Few trains, few buses so that the timetable of the buses are more then one minute long
            var moment = new DateTime(2018, 10, 30, 04, 00, 00);


            var tt = merged.GetTimeTable(moment);
            IConnection prev = null;
            var graph = new List<IConnection>(tt.Connections());
            foreach (var conn in graph)
            {
                Log($"{conn.DepartureTime():HH:mm} {conn.Id()}");
                Assert.True(prev == null || prev.DepartureTime() <= conn.DepartureTime());
                prev = conn;
            }

            var sncbTt = sncb.GetTimeTable(moment);
            Log(
                $"NMBS Table: {sncbTt.StartTime():HH:mm} --> {sncbTt.EndTime():HH:mm}, {sncbTt.Connections().Count()} entries");
            var deLijnTt = deLijn.GetTimeTable(moment);
            Log(
                $"De Lijn Table: {deLijnTt.StartTime():HH:mm} --> {deLijnTt.EndTime():HH:mm}, {deLijnTt.Connections().Count()} entries");

            Log(
                $"Synthetic table with {graph.Count} entries,  starting at {tt.StartTime()} till {tt.EndTime()}");
            Assert.Equal(125, graph.Count);
            Assert.Equal(106, sncbTt.Connections().Count());
            Assert.Equal(73, deLijnTt.Connections().Count());

            var graphR = new List<IConnection>(tt.ConnectionsReversed());

            Assert.Equal(graph.Count, graphR.Count);

            for (var i = 0; i < graph.Count; i++)
            {
                Assert.Equal(graph[i].DepartureTime(), graphR[graph.Count - 1 - i].DepartureTime());
            }
        }
    }
}