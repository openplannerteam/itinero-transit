using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class ConnectionsDbDepartureEnumeratorTest: FunctionalTest<int, ConnectionsDb>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static ConnectionsDbDepartureEnumeratorTest Default => new ConnectionsDbDepartureEnumeratorTest();

        protected override int Execute(ConnectionsDb input)
        {
            // enumerate connections by departure time.
            var tt = 0;
            var ce = 0;
            var departureEnumerator = input.GetDepartureEnumerator();
            departureEnumerator.Reset();
            while (departureEnumerator.MoveNext())
            {
//                    var departureDate = DateTimeExtensions.FromUnixTime(departureEnumerator.DepartureTime);
//                    Log.Information($"Connection {departureEnumerator.GlobalId}: @{departureDate} " +
//                                    $"({departureEnumerator.TravelTime}s [{departureEnumerator.DepartureStop} -> {departureEnumerator.ArrivalStop}])");
                tt += departureEnumerator.TravelTime;
                ce++;
            }
            Information($"Enumerated {ce} connections!");

            while (departureEnumerator.MovePrevious())
            {
                tt -= departureEnumerator.TravelTime;
                ce++;
            }
            Information($"Enumerated back, {tt}");

            return ce;
        }
    }
}