using Itinero.Transit.Data;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionsDbDepartureEnumeratorTest: FunctionalTest<int, TransitDb>
    {
        protected override int Execute(TransitDb input)
        {
            var latest = input.Latest;
            
            // enumerate connections by departure time.
            var tt = 0;
            var ce = 0;
            var enumerator = latest.ConnectionsDb.GetReader();
            var index = enumerator.First().Value;
            while (enumerator.HasNext(index, out index))
            {
                tt += enumerator.Get(index).TravelTime;
                ce++;
            }
            Information($"Enumerated {ce} connections!");

            
            
            Information("Starting Forwards enumeration");
            // enumerate connections by departure time, but in reverse.
            
            var departureEnumerator = latest.ConnectionsDb.GetDepartureEnumerator();
            departureEnumerator.MoveTo(latest.ConnectionsDb.EarliestDate);
            var c = new Connection();
            while (departureEnumerator.HasNext())
            {
                departureEnumerator.Current(c);
                tt -= c.TravelTime;
                ce++;
            }
            Information($"Enumerated forward, {tt}");
            Information("Starting Forwards enumeration");
            // enumerate connections by departure time, but in reverse.
            
            departureEnumerator.MoveTo(latest.ConnectionsDb.LatestDate);
            while (departureEnumerator.HasPrevious())
            {
                departureEnumerator.Current(c);
                tt += c.TravelTime;
                ce++;
            }
            True(tt == 0);
            Information($"Enumerated forward, {tt}");

            return ce;
        }
    }
}