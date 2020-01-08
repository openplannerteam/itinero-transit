using System;
using System.Collections;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class Gtfs2TdbTest
    {

        [Fact]
        public void AddDay_13oct_ConnectionsAreLoaded()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");
            
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            convertor.AddLocations(wr);
            convertor.AddDay(wr, new DateTime(2019,10,13, 0,0,0, DateTimeKind.Utc).Date);
            wr.Close();
            
            
            

        }
        
        // TODO test that trips on 'end_date' are included as well
        
        [Fact]
        public void AgencyURLS_SNCB_ContainBelgianTrainId()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var urls = convertor.AgencyUrls().ToList();

            Assert.Single((IEnumerable) urls);
            Assert.Equal("http://www.belgiantrain.be/", urls[0]);
        }

        [Fact]
        public void IdentifierPrefix_SNCB_BelgianTrail()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var url = convertor.IdentifierPrefix;

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
        public void AddLocations_SNCB_ContainsBruges()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var mapping = convertor.AddLocations(wr);
            wr.Close();

            Assert.Equal(2636, mapping.Count);
            var bruges = tdb.Latest.StopsDb.Get("http://www.belgiantrain.be/stop/8891009");
            Assert.Equal("Bruges", bruges.Attributes["name"]);
        }

        [Fact]
        public void AddLocations_SNCB_TranslationsAreAdded()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var mapping = convertor.AddLocations(wr);
            wr.Close();

            Assert.Equal(2636, mapping.Count);
            var bruges = tdb.Latest.StopsDb.Get("http://www.belgiantrain.be/stop/8891009");
            Assert.Equal("Brugge", bruges.Attributes["name:nl"]);

            var gent = tdb.Latest.StopsDb.Get("http://www.belgiantrain.be/stop/8892007");
            // TODO enable            Assert.Equal("Gent-Sint-Pieters",gent.Attributes["name:nl"]);
        }
    }
}