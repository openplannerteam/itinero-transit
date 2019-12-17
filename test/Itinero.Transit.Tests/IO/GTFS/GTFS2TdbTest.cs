using System.Collections;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class GTFS2TdbTest
    {
        [Fact]
        public void AgencyURLS_SNCB_AContainBelgianTrain()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var urls = convertor.AgencyURLS().ToList();

            Assert.Single((IEnumerable) urls);
            Assert.Equal("http://www.belgiantrain.be/", urls[0]);
        }
        
        [Fact]
        public void IdentifierPrefix_SNCB_BelgianTrail()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var url = convertor.IdentifierPrefix();

            Assert.Equal("http://www.belgiantrain.be/", url);
            Assert.EndsWith("/", url);
        }
        
        [Fact]
        public void AddLocations_SNCB_AllLocationsadded()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            
            var mapping = convertor.AddLocations(wr);
            wr.Close();
            
            Assert.Equal(2636, mapping.Count);
        }
        
        [Fact]
        public void AddTrip()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            
            var mapping = convertor.AddLocations(wr);
            
            convertor.AddServiceForDay();
            
            wr.Close();
            
            Assert.Equal(2636, mapping.Count);
            

        }
    }
}