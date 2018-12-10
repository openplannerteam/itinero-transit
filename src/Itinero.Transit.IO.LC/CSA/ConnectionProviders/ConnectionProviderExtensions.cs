using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Logging;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.IO.LC.CSA.ConnectionProviders
{
    internal static class ConnectionProviderExtensions
    {
        public static ITimeTable GetTimeTable(this IConnectionsProvider prov, DateTime time)
        {
            return prov.GetTimeTable(prov.TimeTableIdFor(time));
        }

        public static List<ITimeTable> DownloadDay(this IConnectionsProvider prov, DateTime start)
        {
            var all = new List<ITimeTable>();
            var tt = prov.GetTimeTable(start);
            all.Add(tt);
            while ((tt.EndTime() - start).Days < 1)
            {
                tt = prov.GetTimeTable(tt.NextTable());
                Log.Information($"Got timetable starting at {tt.StartTime()}");
                all.Add(tt);
            }

            return all;
        }
    }
}