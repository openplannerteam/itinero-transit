using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Xunit;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Tests.Data
{
    public class ReadWriteTest
    {
        [Fact]
        public void TestReadWrite()
        {
            var tdb = new TransitDb();

            var writer = tdb.GetWriter();

            var attrs = new List<Attribute>
            {
                new Attribute("name", "Some Stop"),
                new Attribute("key", "Some value")
            };
            var stop0 = writer.AddOrUpdateStop("http://example.org/stop/0", 5.123, 51.123, attrs);

            attrs = new List<Attribute>
            {
                new Attribute("name", "Some other Stop"),
                new Attribute("key", "Some otter value")
            };
            var stop1 = writer.AddOrUpdateStop("http://example.org/stop/1", 5.456, 51.456, attrs);


            writer.AddOrUpdateConnection(stop0, stop1, "http://example.org/connection/0",
                new DateTime(2019, 03, 06, 10, 00, 00),
                60 * 60,
                0, 0, 0, 0);
            
            writer.AddOrUpdateConnection(stop1, stop0, "http://example.org/connection/1",
                new DateTime(2019, 03, 06, 11, 00, 00),
                30 * 60,
                0, 0, 0, 0);


            writer.Close();

            using (var stream = new MemoryStream())
            {
                tdb.Latest.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var loadedDb = TransitDb.ReadFrom(stream, 0);


                var snapShot = loadedDb.Latest;

                var stopsReader = snapShot.StopsDb.GetReader();
                Assert.True(stopsReader.MoveTo("http://example.org/stop/0"));
                Assert.Equal(5112,(int) (100*stopsReader.Latitude));
                Assert.Equal(512,(int) (100*stopsReader.Longitude));
                stopsReader.Attributes.TryGetValue("name", out var nm);
                Assert.Equal("Some Stop", nm);


                var enumerator = snapShot.ConnectionsDb.GetDepartureEnumerator();
                Assert.True(enumerator.MoveNext(new DateTime(2019, 03, 06, 9, 59, 00)));
                
                Assert.Equal(stop0, enumerator.DepartureStop);
                Assert.Equal(stop1, enumerator.ArrivalStop);
                Assert.Equal(new DateTime(2019, 03, 06, 10, 00, 00).ToUnixTime(), enumerator.DepartureTime);
                Assert.Equal(60*60, enumerator.TravelTime);
                Assert.Equal("http://example.org/connection/0", enumerator.GlobalId);
                
                
                
                
                Assert.True(enumerator.MoveNext());
                
                Assert.Equal(stop1, enumerator.DepartureStop);
                Assert.Equal(stop0, enumerator.ArrivalStop);
                Assert.Equal(new DateTime(2019, 03, 06, 11, 00, 00).ToUnixTime(), enumerator.DepartureTime);
                Assert.Equal(30*60, enumerator.TravelTime);
                Assert.Equal("http://example.org/connection/1", enumerator.GlobalId);


            }
        }
    }
}