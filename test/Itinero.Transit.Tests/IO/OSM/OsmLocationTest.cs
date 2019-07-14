using System;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class OsmLocationTest
    {
        [Fact]
        public void TestMoveToGlobalLocation()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var reader = new OsmLocationStopReader(0);
            reader.MoveTo(loc);


            Assert.True(Math.Abs(51.21575 - reader.Latitude) < 0.000001);
            Assert.True(Math.Abs(3.21999 - reader.Longitude) < 0.000001);
            Assert.Equal(loc, reader.GlobalId);
            
            
        }
        
        [Fact]
        public void TestMoveToLocationId()
        {

            var id = new StopId(0, 141215750, 183219990);
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var reader = new OsmLocationStopReader(0);
            reader.MoveTo(id);


            Assert.True(Math.Abs(51.21575 - reader.Latitude) < 0.000001);
            Assert.True(Math.Abs(3.21999 - reader.Longitude) < 0.000001);
            Assert.Equal(loc, reader.GlobalId);
            Assert.Equal(id, reader.Id);
            
        }


        [Fact]
        public void TestParsing()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var zoomLvl = ParseOsmUrl.ParsePrefix().Parse((loc, 0));
            Assert.Equal(19 , zoomLvl.Result);
            ParseOsmUrl.ParseUrl().ParseFull(loc);
        }
    }
}