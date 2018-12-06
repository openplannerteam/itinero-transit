// The MIT License (MIT)

// Copyright (c) 2018 Anyways B.V.B.A.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Tiles;
using Xunit;
// ReSharper disable UnusedVariable

namespace Itinero.Transit.Tests.Data
{
    public class StopsDbTests
    {
        private const int P = 4; // TODO: this is not good enough!
        
        [Fact]
        public void StopsDb_ShouldStoreWithTiledId()
        {
            var db = new StopsDb();
            var id = db.Add("http://irail.be/stations/NMBS/008863008", 4.786863327026367, 51.26277419739382);

            var tile = Tile.WorldToTile(4.786863327026367, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, id.localTileId);
            Assert.Equal((uint)0, id.localId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldEnumerateStop()
        {
            var db = new StopsDb();
            var id = db.Add("http://irail.be/stations/NMBS/008863008", 4.786863327026367, 51.26277419739382);

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(4.78686332702636700, enumerator.Longitude, P);
            Assert.Equal(51.26277419739382, enumerator.Latitude, P);
            Assert.Equal(id.localTileId, enumerator.Id.localTileId);
            Assert.Equal(id.localId, enumerator.Id.localId);
            Assert.Equal("http://irail.be/stations/NMBS/008863008", enumerator.GlobalId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldEnumerateAllStops()
        {
            var db = new StopsDb();
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var enumerator = db.GetReader();
            var result = new Dictionary<string, (double longitude, double latitude)>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.GlobalId, (enumerator.Longitude, enumerator.Latitude));
            }
            
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldMoveToId()
        {
            var db = new StopsDb();
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo(id4.localTileId, id4.localId));
            Assert.Equal(4.955863952636719, enumerator.Longitude, P);
            Assert.Equal(51.32546294433130, enumerator.Latitude, P);
            Assert.Equal(id4.localTileId, enumerator.Id.localTileId);
            Assert.Equal(id4.localId, enumerator.Id.localId);
            Assert.Equal("http://irail.be/stations/NMBS/008863010", enumerator.GlobalId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldMoveToGlobalId()
        {
            var db = new StopsDb();
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863010"));
            Assert.Equal(4.955863952636719, enumerator.Longitude, P);
            Assert.Equal(51.32546294433130, enumerator.Latitude, P);
            Assert.Equal(id4.localTileId, enumerator.Id.localTileId);
            Assert.Equal(id4.localId, enumerator.Id.localId);
            Assert.Equal("http://irail.be/stations/NMBS/008863010", enumerator.GlobalId);
        }
    }
}