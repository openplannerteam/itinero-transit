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

using Itinero.Transit.Data.Tiles;
using Xunit;

namespace Itinero.Transit.Tests.Data.Tiles
{
    public class TiledLocationIndexTests
    {
        private const int p = 4; // TODO: this is not good enough!
        
        [Fact]
        public void TiledLocationIndex_ShouldStoreLocationInOwnTile()
        {
            var index = new TiledLocationIndex(14);
            var location = index.Add(4.786863327026367, 51.26277419739382);

            var tile = Tile.WorldToTile(4.786863327026367, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location.tileId);
            Assert.Equal((uint)0, location.localId);
        }
        
        [Fact]
        public void TiledLocationIndex_ShouldStoreConsecutiveLocationsInTheirOwnTile()
        {
            var index = new TiledLocationIndex(14);
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);
            
            var tile = Tile.WorldToTile(4.78686332702636700, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location1.tileId);
            Assert.Equal((uint)0, location1.localId);
            
            tile = Tile.WorldToTile(-1.3842773437499998, 47.96050238891509, 14);
            Assert.Equal(tile.LocalId, location2.tileId);
            Assert.Equal((uint)0, location2.localId);
            
            tile = Tile.WorldToTile(12.0410156250000000, 50.16282433381728, 14);
            Assert.Equal(tile.LocalId, location3.tileId);
            Assert.Equal((uint)0, location3.localId);
            
            tile = Tile.WorldToTile(-2.2631835937500000, 53.37022057395678, 14);
            Assert.Equal(tile.LocalId, location4.tileId);
            Assert.Equal((uint)0, location4.localId);
        }
        
        [Fact]
        public void TiledLocationIndex_ShouldStoreLocationsInTheSameTileInTheSameTile()
        {
            var index = new TiledLocationIndex(14);
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(4.780426025390625, 51.26900396316538);
            var location3 = index.Add(4.7783660888671875, 51.2664799356903);
            var location4 = index.Add(4.783945083618164, 51.26970207394979);
            
            var tile = Tile.WorldToTile(4.78686332702636700, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location1.tileId);
            Assert.Equal((uint)0, location1.localId);
            Assert.Equal(tile.LocalId, location2.tileId);
            Assert.Equal((uint)1, location2.localId);
            Assert.Equal(tile.LocalId, location3.tileId);
            Assert.Equal((uint)2, location3.localId);
            Assert.Equal(tile.LocalId, location4.tileId);
            Assert.Equal((uint)3, location4.localId);
        }
        
        [Fact]
        public void TiledLocationIndexEnumerator_ShouldEnumerateLocation()
        {
            var index = new TiledLocationIndex(14);
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);

            var enumerator = index.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(4.78686332702636700, enumerator.Longitude, p);
            Assert.Equal(51.26277419739382, enumerator.Latitude, p);
            Assert.Equal(location1.tileId, enumerator.TileId);
            Assert.Equal(location1.localId, enumerator.LocalId);
            Assert.Equal(location1.dataPointer, enumerator.DataPointer);
        }
        
        [Fact]
        public void TiledLocationIndexEnumerator_ShouldEnumerateAllLocationsPerTile()
        {
            var index = new TiledLocationIndex(14);
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);
            var location5 = index.Add(4.780426025390625, 51.26900396316538);
            var location6 = index.Add(4.7783660888671875, 51.2664799356903);
            var location7 = index.Add(4.783945083618164, 51.26970207394979);

            var enumerator = index.GetEnumerator();
            Assert.True(enumerator.MoveNext()); // should be location1.
            Assert.Equal(4.78686332702636700, enumerator.Longitude, p);
            Assert.Equal(51.26277419739382, enumerator.Latitude, p);
            Assert.Equal(location1.tileId, enumerator.TileId);
            Assert.Equal(location1.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location5.
            Assert.Equal(4.780426025390625, enumerator.Longitude, p);
            Assert.Equal(51.26900396316538, enumerator.Latitude, p);
            Assert.Equal(location5.tileId, enumerator.TileId);
            Assert.Equal(location5.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location6.
            Assert.Equal(4.7783660888671875, enumerator.Longitude, p);
            Assert.Equal(51.2664799356903, enumerator.Latitude, p);
            Assert.Equal(location6.tileId, enumerator.TileId);
            Assert.Equal(location6.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location7.
            Assert.Equal(4.783945083618164, enumerator.Longitude, p);
            Assert.Equal(51.26970207394979, enumerator.Latitude, p);
            Assert.Equal(location7.tileId, enumerator.TileId);
            Assert.Equal(location7.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location2.
            Assert.Equal(-1.3842773437499998, enumerator.Longitude, p);
            Assert.Equal(47.96050238891509, enumerator.Latitude, p);
            Assert.Equal(location2.tileId, enumerator.TileId);
            Assert.Equal(location2.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location3.
            Assert.Equal(12.0410156250000000, enumerator.Longitude, p);
            Assert.Equal(50.16282433381728, enumerator.Latitude, p);
            Assert.Equal(location3.tileId, enumerator.TileId);
            Assert.Equal(location3.localId, enumerator.LocalId);
            
            Assert.True(enumerator.MoveNext()); // should be location4.
            Assert.Equal(-2.2631835937500000, enumerator.Longitude, p);
            Assert.Equal(53.37022057395678, enumerator.Latitude, p);
            Assert.Equal(location4.tileId, enumerator.TileId);
            Assert.Equal(location4.localId, enumerator.LocalId);
            
            Assert.False(enumerator.MoveNext());
        }
    }
}