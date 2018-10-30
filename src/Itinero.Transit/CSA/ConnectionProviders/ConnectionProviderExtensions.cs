using System;
using Serilog;

namespace Itinero.Transit.CSA.ConnectionProviders
{
    public static class ConnectionProviderExtensions
    {
        public static ITimeTable GetTimeTable(this IConnectionsProvider prov, DateTime time)
        {
            return prov.GetTimeTable(prov.TimeTableIdFor(time));
        }

        // ReSharper disable once UnusedMember.Global
        public static void DownloadDay(this IConnectionsProvider prov, DateTime start)
        {
            var all = new Reminiscence.Collections.List<ITimeTable>();
            var tt = prov.GetTimeTable(start);
            all.Add(tt);
            while ((tt.EndTime() - start).Days < 1)
            {
                tt = prov.GetTimeTable(tt.NextTable());
                Log.Information($"Got timetable starting at {tt.StartTime()}");
                all.Add(tt);
            }
        }
    }
}