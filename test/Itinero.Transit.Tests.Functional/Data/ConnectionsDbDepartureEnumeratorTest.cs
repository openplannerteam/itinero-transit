using System;
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
                tt += departureEnumerator.TravelTime;
                ce++;
                if (ce > 1000)
                {
                    break;
                }
            }
            Information($"Enumerated {ce} connections!");

            Information("Starting backwards enumeration");
            // enumerate connections by departure time, but in reverse.
            departureEnumerator = input.GetDepartureEnumerator();
            departureEnumerator.Reset();
            while (departureEnumerator.MovePrevious())
            {
                tt -= departureEnumerator.TravelTime;
                ce++;
                Information($"{ConnectionExtensions.ToString(departureEnumerator)}");
            }
            Information($"Enumerated back, {tt}");

            return ce;
        }
    }
}