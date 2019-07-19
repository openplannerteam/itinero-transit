using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Tests.Functional.Algorithms.Search
{
    public class StopSearchTest : FunctionalTest<IStop, (TransitDb db, double lon, double lat, double distance)>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static StopSearchTest Default => new StopSearchTest();
        
        protected override IStop Execute((TransitDb db, double lon, double lat, double distance) input)
        {
            return input.db.Latest.StopsDb.GetReader().FindClosest(new Stop(input.lon, input.lat), (uint) input.distance);
        }
    }
}