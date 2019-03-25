using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Tiles;
using Xunit;

// ReSharper disable UnusedVariable

namespace Itinero.Transit.Tests.Data
{
    public class StopsDbTests
    {
        private const int _p = 4; // TODO: this is not good enough!

        [Fact]
        public void StopsDb_ShouldStoreWithTiledId()
        {
            var db = new StopsDb(0);
            var id = db.Add("http://irail.be/stations/NMBS/008863008", 4.786863327026367, 51.26277419739382);

            var tile = Tile.WorldToTile(4.786863327026367, 51.26277419739382, 14);
            Assert.Equal(tile.LocalId, id.LocalTileId);
            Assert.Equal((uint) 0, id.LocalId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldEnumerateStop()
        {
            var db = new StopsDb(0);
            var id = db.Add("http://irail.be/stations/NMBS/008863008", 4.786863327026367, 51.26277419739382);

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(4.78686332702636700, enumerator.Longitude, _p);
            Assert.Equal(51.26277419739382, enumerator.Latitude, _p);
            Assert.Equal(id.LocalTileId, enumerator.Id.LocalTileId);
            Assert.Equal(id.LocalId, enumerator.Id.LocalId);
            Assert.Equal("http://irail.be/stations/NMBS/008863008", enumerator.GlobalId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldEnumerateAllStops()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
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
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);
            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo(new LocationId(0, id4.LocalTileId, id4.LocalId)));
            Assert.Equal(4.955863952636719, enumerator.Longitude, _p);
            Assert.Equal(51.32546294433130, enumerator.Latitude, _p);
            Assert.Equal(id4.LocalTileId, enumerator.Id.LocalTileId);
            Assert.Equal(id4.LocalId, enumerator.Id.LocalId);
            Assert.Equal("http://irail.be/stations/NMBS/008863010", enumerator.GlobalId);
        }

        [Fact]
        public void StopsDbEnumerator_ShouldAddAttributes()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382,
                new[] {new Attribute("name", "Jambes-Est")});
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885,
                new[] {new Attribute("name", "Namur")});
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275,
                new[] {new Attribute("name", "Genk")});
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313,
                new[] {new Attribute("name", "Antwerpen")});
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337,
                new[] {new Attribute("name", "Brussel-Zuid")});
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494,
                new[] {new Attribute("name", "Oostende")});

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863010"));
            Assert.Equal(new AttributeCollection(new Attribute("name", "Antwerpen")).ToString(),
                enumerator.Attributes.ToString());
            Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863012"));
            Assert.Equal(new AttributeCollection(new Attribute("name", "Oostende")).ToString(),
                enumerator.Attributes.ToString());
            Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863354"));
            Assert.Equal(new AttributeCollection(new Attribute("name", "Jambes-Est")).ToString(),
                enumerator.Attributes.ToString());
        }

        [Fact]
        public void TiledLocationIndex_WriteToReadFromShouldBeCopy()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382,
                new[] {new Attribute("name", "Jambes-Est")});
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885,
                new[] {new Attribute("name", "Namur")});
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275,
                new[] {new Attribute("name", "Genk")});
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313,
                new[] {new Attribute("name", "Antwerpen")});
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337,
                new[] {new Attribute("name", "Brussel-Zuid")});
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494,
                new[] {new Attribute("name", "Oostende")});

            using (var stream = new MemoryStream())
            {
                var size = db.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                db = StopsDb.ReadFrom(stream, 0);

                var enumerator = db.GetReader();
                Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863010"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Antwerpen")).ToString(),
                    enumerator.Attributes.ToString());
                Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863012"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Oostende")).ToString(),
                    enumerator.Attributes.ToString());
                Assert.True(enumerator.MoveTo("http://irail.be/stations/NMBS/008863354"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Jambes-Est")).ToString(),
                    enumerator.Attributes.ToString());
            }
        }

        [Fact]
        public void CloseToEachOtherTest()
        {
            var tdb = new TransitDb();
            var wr = tdb.GetWriter();
            wr.AddOrUpdateStop("http://example.org/stop/1", (float) 5.0001, (float) 51.0001, new List<Attribute>
            {
                new Attribute("name", "Brugge")
            });

            wr.AddOrUpdateStop("http://example.org/stop/2", (float) 5.0003, (float) 51.0003, new List<Attribute>
            {
                new Attribute("name", "Helemaal niet Brugge")
            });
            wr.Close();

            var stopsReader = tdb.Latest.StopsDb.GetReader();

            Assert.True(stopsReader.MoveNext());
            Assert.Equal("http://example.org/stop/1", stopsReader.GlobalId);
            stopsReader.Attributes.TryGetValue("name", out var name);
            Assert.Equal("Brugge", name);

            Assert.True(stopsReader.MoveNext());
            Assert.Equal("http://example.org/stop/2", stopsReader.GlobalId);
            stopsReader.Attributes.TryGetValue("name", out name);
            Assert.Equal("Helemaal niet Brugge", name);

            Assert.False(stopsReader.MoveNext());
        }


        [Fact]
        public void FarFromEachOtherTest()
        {
            var tdb = new TransitDb();
            var wr = tdb.GetWriter();
            // THIS TEST IS NEARLY IDENTICAL TO THE ONE ABOVE but   ↓ this value is different
            wr.AddOrUpdateStop("http://example.org/stop/1", (float) 6.0001, (float) 51.0001, new List<Attribute>
            {
                new Attribute("name", "Brugge")
            });

            wr.AddOrUpdateStop("http://example.org/stop/2", (float) 5.0003, (float) 51.0003, new List<Attribute>
            {
                new Attribute("name", "Helemaal niet Brugge")
            });
            wr.Close();

            var stopsReader = tdb.Latest.StopsDb.GetReader();

            Assert.True(stopsReader.MoveNext());
            Assert.Equal("http://example.org/stop/1", stopsReader.GlobalId);
            stopsReader.Attributes.TryGetValue("name", out var name);
            Assert.Equal("Brugge", name);

            Assert.True(stopsReader.MoveNext());
            Assert.Equal("http://example.org/stop/2", stopsReader.GlobalId);
            stopsReader.Attributes.TryGetValue("name", out name);
            Assert.Equal("Helemaal niet Brugge", name);

            Assert.False(stopsReader.MoveNext());
        }
    }
}