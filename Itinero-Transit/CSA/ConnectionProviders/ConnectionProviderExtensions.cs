using System;
using System.Collections.Generic;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public static class ConnectionProviderExtensions
    {
        public static ITimeTable GetTimeTable(this IConnectionsProvider prov, DateTime time)
        {
            return prov.GetTimeTable(prov.TimeTableIdFor(time));
        }

        public static Reminiscence.Collections.List<ITimeTable> DownloadDay(this IConnectionsProvider prov, DateTime start)
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

            return all;
        }

        public static Location GetCoordinateFor(this IConnectionsProvider prov, Uri id)
        {
            return prov.LocationProvider().GetCoordinateFor(id);
        }

        public static IEnumerable<Uri> GetLocationsCloseTo(this IConnectionsProvider prov, float lat, float lon, int withinMeters)
        {
            return prov.LocationProvider().GetLocationsCloseTo(lat, lon, withinMeters);
        }
    }
}