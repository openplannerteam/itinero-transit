using System;
using System.Collections;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Itinero.Transit.IO.GTFS.Data;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class Gtfs2TdbTest
    {
        [Fact]
        public void AddDay_13oct_ConnectionsAreLoaded()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var wr = TransitDbSnapShot.CreateCompactedWriter(0, "https://sncb.be/");
            convertor.AddLocations(wr);
            var d = new DateTime(2019, 10, 13, 0, 0, 0, DateTimeKind.Unspecified).Date;
            convertor.AddDay(wr, d, d, d.AddDays(2));
            var tdb = ((IWriter) wr).GetSnapshot();
            
            Assert.True(tdb.Connections.Count() > 10000);
            Assert.True(tdb.Connections.EarliestDate.FromUnixTime() <= d.Date.AddMinutes(5));
        }

        [Fact]
        public void LoadTimePeriod_HourWithinGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.UseGtfs("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 10, 21, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 10, 21, 11, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.Connections.Count();
            Assert.Equal(3597, count);
        }
        
        
        [Fact]
        public void LoadTimePeriod_HourAtFirstDayOfGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.UseGtfs("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 10, 07, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 10, 07, 11, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.Connections.Count();
            Assert.Equal(3558, count);
        }
        
        
        [Fact]
        public void LoadTimePeriod_HourAtLastDayOfGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.UseGtfs("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 12, 13, 23, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 12, 14, 00, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.Connections.Count();
            Assert.Equal(50, count);
        }

        [Fact]
        public void AgencyURLS_SNCB_ContainBelgianTrainId()
        {
            var convertor = new FeedData("IO/GTFS/sncb-13-october.zip");

            var urls = convertor.AgencyUrls().ToList();

            Assert.Single((IEnumerable) urls);
            Assert.Equal("http://www.belgiantrain.be/", urls[0]);
        }

        [Fact]
        public void IdentifierPrefix_SNCB_BelgianTrail()
        {
            var convertor = new FeedData("IO/GTFS/sncb-13-october.zip");

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
            tdb.CloseWriter();

            Assert.Equal(2636, mapping.Count);
        }

        [Fact]
        public void AddLocations_SNCB_ContainsBruges()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var mapping = convertor.AddLocations(wr);
            tdb.CloseWriter();

            Assert.Equal(2636, mapping.Count);
            var bruges = tdb.Latest.Stops.Get("http://www.belgiantrain.be/stop/8891009");
            Assert.Equal("Bruges", bruges.Attributes["name"]);
        }

        [Fact]
        public void AddLocations_SNCB_TranslationsAreAdded()
        {
            var convertor = new Gtfs2Tdb("IO/GTFS/sncb-13-october.zip");

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var mapping = convertor.AddLocations(wr);
            tdb.CloseWriter();

            Assert.Equal(2636, mapping.Count);
            var bruges = tdb.Latest.Stops.Get("http://www.belgiantrain.be/stop/8891009");
            Assert.Equal("Brugge", bruges.Attributes["name:nl"]);

            var gent = tdb.Latest.Stops.Get("http://www.belgiantrain.be/stop/8892007");
            // TODO enable            Assert.Equal("Gent-Sint-Pieters",gent.Attributes["name:nl"]);
        }
    }
}