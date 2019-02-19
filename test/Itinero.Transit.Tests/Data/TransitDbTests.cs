using System;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Xunit;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Tests.Data
{
    public class TransitDbTests
    {
        [Fact]
        public void TransitDb_WriteToReadFromShouldBeCopy()
        {
            var db = new TransitDb();

            var writer = db.GetWriter();

            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.26277419739382,
                new[] {new Attribute("name", "Jambes-Est")});
            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885,
                new[] {new Attribute("name", "Namur")});
            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.22365776470275,
                new[] {new Attribute("name", "Genk")});
            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.3254629443313,
                new[] {new Attribute("name", "Antwerpen")});
            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.37328062064337,
                new[] {new Attribute("name", "Brussel-Zuid")});
            writer.AddOrUpdateStop("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494,
                new[] {new Attribute("name", "Oostende")});

            writer.AddOrUpdateTrip("http://irail.be/vehicle/IC725",
                new[]
                {
                    new Attribute("headsign", "IC725"), new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal")
                });
            writer.AddOrUpdateTrip("http://irail.be/vehicle/IC704",
                new[] {new Attribute("headsign", "IC704"), new Attribute("name", "Antwerpen-Centraal - Poperinge")});

            writer.AddOrUpdateConnection((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            writer.AddOrUpdateConnection((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9), 54, 10245);
            writer.AddOrUpdateConnection((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            writer.AddOrUpdateConnection((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9), 4500, 10245);
            writer.AddOrUpdateConnection((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            writer.Close();

            using (var stream = new MemoryStream())
            {
                db.Latest.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                var newDb = TransitDb.ReadFrom(stream);

                var latest = newDb.Latest;

                var stopsDbReader = latest.StopsDb.GetReader();
                Assert.True(stopsDbReader.MoveTo("http://irail.be/stations/NMBS/008863010"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Antwerpen")).ToString(),
                    stopsDbReader.Attributes.ToString());
                Assert.True(stopsDbReader.MoveTo("http://irail.be/stations/NMBS/008863012"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Oostende")).ToString(),
                    stopsDbReader.Attributes.ToString());
                Assert.True(stopsDbReader.MoveTo("http://irail.be/stations/NMBS/008863354"));
                Assert.Equal(new AttributeCollection(new Attribute("name", "Jambes-Est")).ToString(),
                    stopsDbReader.Attributes.ToString());

                var tripsDbReader = latest.TripsDb.GetReader();
                Assert.True(tripsDbReader.MoveTo("http://irail.be/vehicle/IC725"));
                Assert.Equal("http://irail.be/vehicle/IC725", tripsDbReader.GlobalId);
                Assert.Equal(
                    new AttributeCollection(new Attribute("headsign", "IC725"),
                        new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal")),
                    tripsDbReader.Attributes);
                Assert.True(tripsDbReader.MoveTo("http://irail.be/vehicle/IC704"));
                Assert.Equal("http://irail.be/vehicle/IC704", tripsDbReader.GlobalId);
                Assert.Equal(
                    new AttributeCollection(new Attribute("headsign", "IC704"),
                        new Attribute("name", "Antwerpen-Centraal - Poperinge")),
                    tripsDbReader.Attributes);

                var departureEnumerator = latest.ConnectionsDb.GetDepartureEnumerator();
                Assert.NotNull(departureEnumerator);
                Assert.True(departureEnumerator.MovePrevious(new DateTime(2018, 11, 16)));
                Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", departureEnumerator.GlobalId);
                Assert.False(departureEnumerator.MovePrevious());

                Assert.True(departureEnumerator.MovePrevious(new DateTime(2018, 11, 14, 11, 0, 0)));
                Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", departureEnumerator.GlobalId);
                Assert.True(departureEnumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", departureEnumerator.GlobalId);
                Assert.False(departureEnumerator.MovePrevious());

                Assert.True(departureEnumerator.MovePrevious(new DateTime(2018, 11, 14, 0, 0, 0)));
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", departureEnumerator.GlobalId);
                Assert.False(departureEnumerator.MovePrevious());
            }
        }
        
        [Fact]
        public void TransitDb_ShouldStoreIdenticalGlobalIdsWithIdenticalId()
        {
            var db = new TransitDb();

            var writer = db.GetWriter();

            var globalIds = new []
            {
                "http://irail.be/stations/NMBS/008863008",
                "http://irail.be/stations/NMBS/008863010",
                "http://irail.be/stations/NMBS/008863012",
                "http://irail.be/stations/NMBS/008863014",
                "http://irail.be/stations/NMBS/008863015",
                "http://irail.be/stations/NMBS/008863016",
                "http://irail.be/stations/NMBS/008863017",
                "http://irail.be/stations/NMBS/008863018",
                "http://irail.be/stations/NMBS/008863019",
                "http://irail.be/stations/NMBS/008863020",
                "http://irail.be/stations/NMBS/008863021"
            };

            foreach (var globalId in globalIds)
            {
                var id = writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382);
            
                Assert.Equal(id, writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382));
                Assert.Equal(id, writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382));
                Assert.Equal(id, writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382));
                Assert.Equal(id, writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382));
                Assert.Equal(id, writer.AddOrUpdateStop(globalId, 4.786863327026367, 51.26277419739382));
            }
        }
    }
}