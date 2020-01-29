using GTFS;
using GTFS.Entities;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class TransitDbExtensionsTests
    {
        [Fact]
        public void TransitDbExtensions_AddAgencies_NoAgencies_ShouldDoNothing()
        {
            var feed = new GTFSFeed();

            var transitdb = new TransitDb(0);
            var writer = transitdb.GetWriter();
            writer.AddAgencies(feed);
            writer.Close();

            Assert.Empty(transitdb.Latest.Attributes);
        }
        
        [Fact]
        public void TransitDbExtensions_AddAgencies_OneAgency_ShouldLoadAttributes()
        {
            var feed = new GTFSFeed();
            feed.Agencies.Add(new Agency()
            {
                Id = "agency1"
            });

            var transitdb = new TransitDb(0);
            var writer = transitdb.GetWriter();
            writer.AddAgencies(feed);
            writer.Close();

            Assert.NotEmpty(transitdb.Latest.Attributes);
        }
    }
}