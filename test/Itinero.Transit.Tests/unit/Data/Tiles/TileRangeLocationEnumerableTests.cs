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

using System.Linq;
using Itinero.Transit.Data.Tiles;
using Xunit;

namespace Itinero.Transit.Tests.Data.Tiles
{
    public class TileRangeLocationEnumerableTests
    {
        [Fact]
        public void TileRangeLocationEnumerable_ShouldEnumerateOneInTileRange()
        {
            var index = new TiledLocationIndex(14);
            var location = index.Add(4.786863327026367, 51.26277419739382);
            var tile = Tile.WorldToTile(4.786863327026367, 51.26277419739382, 14);

            var locations = index.GetTileRangeEnumerator(new TileRange((tile.Left, tile.Bottom, tile.Right, tile.Top), 14));
            Assert.NotNull(locations);

            var locationsList = locations.ToList();
            Assert.Equal(1, locationsList.Count);
            Assert.Equal(location.tileId, locationsList[0].tileId);
            Assert.Equal(location.localId, locationsList[0].localId);
        }
        
        [Fact]
        public void TileRangeLocationEnumerable_ShouldEnumerateNoneOutsideTileRange()
        {
            var index = new TiledLocationIndex(14);
            var location = index.Add(4.786863327026367, 51.26277419739382);
            var tile = Tile.WorldToTile( 51.26277419739382, 4.786863327026367, 14);

            var locations = index.GetTileRangeEnumerator(new TileRange((tile.Left, tile.Bottom, tile.Right, tile.Top), 14));
            Assert.NotNull(locations);

            var locationsList = locations.ToList();
            Assert.Equal(0, locationsList.Count);
        }
    }
}