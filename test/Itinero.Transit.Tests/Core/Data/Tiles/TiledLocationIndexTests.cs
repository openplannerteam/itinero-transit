using System.IO;
using Itinero.Transit.Data.Tiles;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data.Tiles
{
    public class TiledLocationIndexTests
    {
        // Accuracy which is used for comparisons
        private const int _p = 4; // TODO: this is not good enough!

        [Fact]
        public void TiledLocationIndex_ShouldStoreLocationInOwnTile()
        {
            var index = new TiledLocationIndex();
            var location = index.Add(4.786863327026367, 51.26277419739382);

            var tile = Tile.WorldToTile(4.786863327026367, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location.tileId);
            Assert.Equal((uint) 0, location.localId);
        }

        [Fact]
        public void TiledLocationIndex_ShouldStoreConsecutiveLocationsInTheirOwnTile()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);

            var tile = Tile.WorldToTile(4.78686332702636700, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location1.tileId);
            Assert.Equal((uint) 0, location1.localId);

            tile = Tile.WorldToTile(-1.3842773437499998, 47.96050238891509, 14);
            Assert.Equal(tile.LocalId, location2.tileId);
            Assert.Equal((uint) 0, location2.localId);

            tile = Tile.WorldToTile(12.0410156250000000, 50.16282433381728, 14);
            Assert.Equal(tile.LocalId, location3.tileId);
            Assert.Equal((uint) 0, location3.localId);

            tile = Tile.WorldToTile(-2.2631835937500000, 53.37022057395678, 14);
            Assert.Equal(tile.LocalId, location4.tileId);
            Assert.Equal((uint) 0, location4.localId);
        }

        [Fact]
        public void TiledLocationIndex_ShouldStoreLocationsInTheSameTileInTheSameTile()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(4.780426025390625, 51.26900396316538);
            var location3 = index.Add(4.7783660888671875, 51.2664799356903);
            var location4 = index.Add(4.783945083618164, 51.26970207394979);

            var tile = Tile.WorldToTile(4.78686332702636700, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, location1.tileId);
            Assert.Equal((uint) 0, location1.localId);
            Assert.Equal(tile.LocalId, location2.tileId);
            Assert.Equal((uint) 1, location2.localId);
            Assert.Equal(tile.LocalId, location3.tileId);
            Assert.Equal((uint) 2, location3.localId);
            Assert.Equal(tile.LocalId, location4.tileId);
            Assert.Equal((uint) 3, location4.localId);
        }

        [Fact]
        public void TiledLocationIndexEnumerator_ShouldEnumerateLocation()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);

            var enumerator = index.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(4.78686332702636700, enumerator.Longitude, _p);
            Assert.Equal(51.26277419739382, enumerator.Latitude, _p);
            Assert.Equal(location1.tileId, enumerator.TileId);
            Assert.Equal(location1.localId, enumerator.LocalId);
            Assert.Equal(location1.dataPointer, enumerator.DataPointer);
        }

        [Fact]
        public void TiledLocationIndexEnumerator_ShouldEnumerateAllLocationsPerTile()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);
            var location5 = index.Add(4.780426025390625, 51.26900396316538);
            var location6 = index.Add(4.7783660888671875, 51.2664799356903);
            var location7 = index.Add(4.783945083618164, 51.26970207394979);

            var enumerator = index.GetEnumerator();
            Assert.True(enumerator.MoveNext()); // should be location1.
            Assert.Equal(4.78686332702636700, enumerator.Longitude, _p);
            Assert.Equal(51.26277419739382, enumerator.Latitude, _p);
            Assert.Equal(location1.tileId, enumerator.TileId);
            Assert.Equal(location1.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location5.
            Assert.Equal(4.780426025390625, enumerator.Longitude, _p);
            Assert.Equal(51.26900396316538, enumerator.Latitude, _p);
            Assert.Equal(location5.tileId, enumerator.TileId);
            Assert.Equal(location5.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location6.
            Assert.Equal(4.7783660888671875, enumerator.Longitude, _p);
            Assert.Equal(51.2664799356903, enumerator.Latitude, _p);
            Assert.Equal(location6.tileId, enumerator.TileId);
            Assert.Equal(location6.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location7.
            Assert.Equal(4.783945083618164, enumerator.Longitude, _p);
            Assert.Equal(51.26970207394979, enumerator.Latitude, _p);
            Assert.Equal(location7.tileId, enumerator.TileId);
            Assert.Equal(location7.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location2.
            Assert.Equal(-1.3842773437499998, enumerator.Longitude, _p);
            Assert.Equal(47.96050238891509, enumerator.Latitude, _p);
            Assert.Equal(location2.tileId, enumerator.TileId);
            Assert.Equal(location2.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location3.
            Assert.Equal(12.0410156250000000, enumerator.Longitude, _p);
            Assert.Equal(50.16282433381728, enumerator.Latitude, _p);
            Assert.Equal(location3.tileId, enumerator.TileId);
            Assert.Equal(location3.localId, enumerator.LocalId);

            Assert.True(enumerator.MoveNext()); // should be location4.
            Assert.Equal(-2.2631835937500000, enumerator.Longitude, _p);
            Assert.Equal(53.37022057395678, enumerator.Latitude, _p);
            Assert.Equal(location4.tileId, enumerator.TileId);
            Assert.Equal(location4.localId, enumerator.LocalId);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TiledLocationIndex_ShouldReadV1()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);
            var location5 = index.Add(4.780426025390625, 51.26900396316538);
            var location6 = index.Add(4.7783660888671875, 51.2664799356903);
            var location7 = index.Add(4.783945083618164, 51.26970207394979);
            
            // load the data as it would have been serialized in v1.
            var v1Data = TestDataHelper.LoadEmbeddedResource("Itinero.Transit.Tests.test_data.v1.tileidx");

            using (var stream = new MemoryStream(v1Data))
            {
                var copy = TiledLocationIndex.ReadFrom(stream);

                var enumerator = copy.GetEnumerator();
                Assert.True(enumerator.MoveNext()); // should be location1.
                Assert.Equal(4.78686332702636700, enumerator.Longitude, _p);
                Assert.Equal(51.26277419739382, enumerator.Latitude, _p);
                Assert.Equal(location1.tileId, enumerator.TileId);
                Assert.Equal(location1.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location5.
                Assert.Equal(4.780426025390625, enumerator.Longitude, _p);
                Assert.Equal(51.26900396316538, enumerator.Latitude, _p);
                Assert.Equal(location5.tileId, enumerator.TileId);
                Assert.Equal(location5.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location6.
                Assert.Equal(4.7783660888671875, enumerator.Longitude, _p);
                Assert.Equal(51.2664799356903, enumerator.Latitude, _p);
                Assert.Equal(location6.tileId, enumerator.TileId);
                Assert.Equal(location6.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location7.
                Assert.Equal(4.783945083618164, enumerator.Longitude, _p);
                Assert.Equal(51.26970207394979, enumerator.Latitude, _p);
                Assert.Equal(location7.tileId, enumerator.TileId);
                Assert.Equal(location7.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location2.
                Assert.Equal(-1.3842773437499998, enumerator.Longitude, _p);
                Assert.Equal(47.96050238891509, enumerator.Latitude, _p);
                Assert.Equal(location2.tileId, enumerator.TileId);
                Assert.Equal(location2.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location3.
                Assert.Equal(12.0410156250000000, enumerator.Longitude, _p);
                Assert.Equal(50.16282433381728, enumerator.Latitude, _p);
                Assert.Equal(location3.tileId, enumerator.TileId);
                Assert.Equal(location3.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location4.
                Assert.Equal(-2.2631835937500000, enumerator.Longitude, _p);
                Assert.Equal(53.37022057395678, enumerator.Latitude, _p);
                Assert.Equal(location4.tileId, enumerator.TileId);
                Assert.Equal(location4.localId, enumerator.LocalId);

                Assert.False(enumerator.MoveNext());
            }
        }

        [Fact]
        public void TiledLocationIndex_WriteToReadFromShouldBeCopy()
        {
            var index = new TiledLocationIndex();
            var location1 = index.Add(4.78686332702636700, 51.26277419739382);
            var location2 = index.Add(-1.3842773437499998, 47.96050238891509);
            var location3 = index.Add(12.0410156250000000, 50.16282433381728);
            var location4 = index.Add(-2.2631835937500000, 53.37022057395678);
            var location5 = index.Add(4.780426025390625, 51.26900396316538);
            var location6 = index.Add(4.7783660888671875, 51.2664799356903);
            var location7 = index.Add(4.783945083618164, 51.26970207394979);

            using (var stream = new MemoryStream())
            {
                index.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                var copy = TiledLocationIndex.ReadFrom(stream);

                var enumerator = copy.GetEnumerator();
                Assert.True(enumerator.MoveNext()); // should be location1.
                Assert.Equal(4.78686332702636700, enumerator.Longitude, _p);
                Assert.Equal(51.26277419739382, enumerator.Latitude, _p);
                Assert.Equal(location1.tileId, enumerator.TileId);
                Assert.Equal(location1.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location5.
                Assert.Equal(4.780426025390625, enumerator.Longitude, _p);
                Assert.Equal(51.26900396316538, enumerator.Latitude, _p);
                Assert.Equal(location5.tileId, enumerator.TileId);
                Assert.Equal(location5.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location6.
                Assert.Equal(4.7783660888671875, enumerator.Longitude, _p);
                Assert.Equal(51.2664799356903, enumerator.Latitude, _p);
                Assert.Equal(location6.tileId, enumerator.TileId);
                Assert.Equal(location6.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location7.
                Assert.Equal(4.783945083618164, enumerator.Longitude, _p);
                Assert.Equal(51.26970207394979, enumerator.Latitude, _p);
                Assert.Equal(location7.tileId, enumerator.TileId);
                Assert.Equal(location7.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location2.
                Assert.Equal(-1.3842773437499998, enumerator.Longitude, _p);
                Assert.Equal(47.96050238891509, enumerator.Latitude, _p);
                Assert.Equal(location2.tileId, enumerator.TileId);
                Assert.Equal(location2.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location3.
                Assert.Equal(12.0410156250000000, enumerator.Longitude, _p);
                Assert.Equal(50.16282433381728, enumerator.Latitude, _p);
                Assert.Equal(location3.tileId, enumerator.TileId);
                Assert.Equal(location3.localId, enumerator.LocalId);

                Assert.True(enumerator.MoveNext()); // should be location4.
                Assert.Equal(-2.2631835937500000, enumerator.Longitude, _p);
                Assert.Equal(53.37022057395678, enumerator.Latitude, _p);
                Assert.Equal(location4.tileId, enumerator.TileId);
                Assert.Equal(location4.localId, enumerator.LocalId);

                Assert.False(enumerator.MoveNext());
            }
        }
    }
}