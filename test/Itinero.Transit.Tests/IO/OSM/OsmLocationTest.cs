using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class OsmLocationTest
    {
        [Fact]
        public void OsmLocationsStopReader_MoveToOsmURL_ExpectsMoved()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var reader = new OsmLocationStopReader(0);
            reader.SearchId(loc, out var id);
            var stop = reader.Get(id);


            Assert.True(Math.Abs(51.21575 - stop.Latitude) < 0.000001);
            Assert.True(Math.Abs(3.21999 - stop.Longitude) < 0.000001);
            Assert.Equal("https://www.openstreetmap.org/#map=19/51.215750000000014/3.2199899999999957", stop.GlobalId);
        }
        
        [Fact]
        public void ParseOsmUrl_ValidInput_ExpectsParse()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var zoomLvl = ParseOsmUrl.ParsePrefix().Parse((loc, 0));
            Assert.Equal(19 , zoomLvl.Result);
            ParseOsmUrl.ParseUrl().ParseFull(loc);
        }
        
    }
}