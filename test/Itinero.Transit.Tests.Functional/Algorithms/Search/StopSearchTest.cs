using Itinero.Transit.Data;

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
            return input.db.Latest.StopsDb.GetReader().SearchClosest(input.lon, input.lat, input.distance);
        }
    }
}