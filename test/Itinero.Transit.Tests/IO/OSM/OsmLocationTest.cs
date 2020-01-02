using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class OsmLocationTest
    {
        [Fact]
        public void SearchId_Bruges_ExpectsCorrectStop()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21575/3.21999";
            var reader = new OsmLocationStopReader(0);
            reader.TryGetId(loc, out var id);
            var stop = reader.Get(id);


            Assert.True(Math.Abs(51.21575 - stop.Latitude) < 0.000001);
            Assert.True(Math.Abs(3.21999 - stop.Longitude) < 0.000001);
            Assert.Equal("https://www.openstreetmap.org/#map=19/51.215750/3.219990", stop.GlobalId);
        }

        [Fact]
        public void SearchId_IsStable()
        {
            var c = (50.00005, 49.99953);
            var reader = new OsmLocationStopReader(0);
            var stop0Id = reader.SearchId(c);
            var stop0 = reader.Get(stop0Id);

            var stop1Id = reader.SearchId(stop0.GlobalId);
            var stop1 = reader.Get(stop1Id);

            var stop2Id = reader.SearchId(stop1.GlobalId);
            var stop2 = reader.Get(stop2Id);

            Assert.Equal(stop1.GlobalId, stop2.GlobalId);
            Assert.Equal(stop0.GlobalId, stop1.GlobalId);
        }

        [Fact]
        public void SearchID_IdIsStable()
        {
            var reader = new OsmLocationStopReader(0, new List<(double lon, double lat)> {(50.00005, 49.99953)});
            var stop = reader.SearchableLocations[0];
            var altStop = reader.Get(stop.GlobalId);

            Assert.Equal(stop.GlobalId, altStop.GlobalId);
            Assert.Equal(stop.Latitude, altStop.Latitude);
            Assert.Equal(stop.Longitude, altStop.Longitude);


            var stops = new List<StopId>();
            reader.TryGetId(stop.GlobalId, out var stopId);
            reader.TryGetId(altStop.GlobalId, out var altStopId);

            stops.Add(stopId);
            Assert.Contains(altStopId, stops);
        }

        [Fact]
        public void SearchId_FloatingZeroes_ExpectsCorrectStop()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.002/3.09";
            var reader = new OsmLocationStopReader(0);
            reader.TryGetId(loc, out var id);
            var stop = reader.Get(id);


            Assert.True(Math.Abs(51.002 - stop.Latitude) < 0.000001);
            Assert.True(Math.Abs(3.09 - stop.Longitude) < 0.000001);
            Assert.Equal("https://www.openstreetmap.org/#map=19/51.002000/3.090000", stop.GlobalId);
        }

        [Fact]
        public void SearchId_ExtremePointPos_ExpectsCorrectStop()
        {
            var loc = "https://www.openstreetmap.org/#map=19/85/180";
            var reader = new OsmLocationStopReader(0);
            reader.TryGetId(loc, out var id);
            var stop = reader.Get(id);


            Assert.True(Math.Abs(85 - stop.Latitude) < 0.000001);
            Assert.True(Math.Abs(180 - stop.Longitude) < 0.000001);
            Assert.Equal("https://www.openstreetmap.org/#map=19/85.000000/180.000000", stop.GlobalId);
        }

        [Fact]
        public void SearchId_ExtremePointNeg_ExpectsCorrectStop()
        {
            var loc = "https://www.openstreetmap.org/#map=19/-85/-180";
            var reader = new OsmLocationStopReader(0);
            reader.TryGetId(loc, out var id);
            var stop = reader.Get(id);


            Assert.True(Math.Abs(85 + stop.Latitude) < 0.000001);
            Assert.True(Math.Abs(180 + stop.Longitude) < 0.000001);
            Assert.Equal("https://www.openstreetmap.org/#map=19/-85.000000/-180.000000", stop.GlobalId);
        }

        [Fact]
        public void ParseOsmUrl_ValidInput_ExpectsParse()
        {
            var loc = "https://www.openstreetmap.org/#map=19/51.21/3.21999";
            var zoomLvl = ParseOsmUrl.ParsePrefix().Parse((loc, 0));
            Assert.Equal(19, zoomLvl.Result);
            var (lonLong, latLong) = ParseOsmUrl.ParseUrl(1000000).ParseFull(loc);
            
            Assert.Equal(3219990, lonLong);
            Assert.Equal(51210000, latLong);
        }
    }
}