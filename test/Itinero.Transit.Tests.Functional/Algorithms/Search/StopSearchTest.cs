using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.Algorithms.Search
{
    public class StopSearchTest : FunctionalTest<IStop, (StopsDb db, double lon, double lat, double distance)>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static StopSearchTest Default => new StopSearchTest();
        
        protected override IStop Execute((StopsDb db, double lon, double lat, double distance) input)
        {
            return input.db.SearchClosest(input.lon, input.lat, input.distance);
        }
    }
}