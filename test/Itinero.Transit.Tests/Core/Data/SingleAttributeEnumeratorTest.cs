using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class SingleAttributeEnumeratorTest
    {
        private void Test(IStop source)
        {
            source.Attributes.TryGetValue("name", out var name);
            if (name != null)
            {
                Assert.NotEmpty(name);
            }

            foreach (var attr in source.Attributes)
            {
                if (attr.Key.StartsWith("name:"))
                {
                    var v = attr.Value;
                    var k = attr.Key;
                    Assert.StartsWith("name:", k);
                    Assert.NotEmpty(v);
                }
            }
        }

        [Fact]
        public void TestEnumerator()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var a = wr.AddOrUpdateStop("a", 1, 1);
            var b = wr.AddOrUpdateStop("b", 1, 1, new List<Attribute>());
            var c = wr.AddOrUpdateStop("c", 1, 1, new List<Attribute>
            {
                new Attribute("name", "c")
            });
            var d = wr.AddOrUpdateStop("d", 1, 1, new List<Attribute>
            {
                new Attribute("name", "d"),
                new Attribute("name:fr", "dfr")
            });
            var e = wr.AddOrUpdateStop("e", 1, 1, new List<Attribute>
            {
                new Attribute("name", "d"),
                new Attribute("name:", "d:")
            });
            var f = wr.AddOrUpdateStop("f", 1, 1, new List<Attribute>
            {
                new Attribute("bus", "yes"),
                new Attribute("name", "couseaukaai"),
                new Attribute("operator", "Stad Brugge"),
                new Attribute("public_transport", "stop_position")
            });
            wr.Close();


            var reader = tdb.Latest.StopsDb.GetReader();

            reader.MoveTo(a);
            Test(reader);
            reader.MoveTo(b);
            Test(reader);
            reader.MoveTo(c);
            Test(reader);
            reader.MoveTo(d);
            Test(reader);
            reader.MoveTo(e);
            Test(reader);
            reader.MoveTo(f);
            Test(reader);
        }
    }
}