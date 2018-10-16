using System;
using Itinero_Transit.CSA;
using Reminiscence.Collections;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public static class ConnectionProviderExtensions
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