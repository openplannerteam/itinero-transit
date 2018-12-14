using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class TripsDbTests
    {
        [Fact]
        public void StopsDbEnumerator_ShouldMoveToId()
        {
            var db = new TripsDb();
            var id1 = db.Add("http://irail.be/vehicle/IC725", 
                new Attribute("headsign", "IC725"), new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal"));
            var id2 = db.Add("http://irail.be/vehicle/IC704", 
                new Attribute("headsign", "IC704"), new Attribute("name", "Antwerpen-Centraal - Poperinge"));

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo(id1));
            Assert.Equal("http://irail.be/vehicle/IC725", enumerator.GlobalId);
            Assert.Equal(new AttributeCollection(new Attribute("headsign", "IC725"), new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal")), 
                enumerator.Attributes);
            Assert.True(enumerator.MoveTo(id2));
            Assert.Equal("http://irail.be/vehicle/IC704", enumerator.GlobalId);
            Assert.Equal(new AttributeCollection(new Attribute("headsign", "IC704"), new Attribute("name", "Antwerpen-Centraal - Poperinge")), 
                enumerator.Attributes);
        }
        
        [Fact]
        public void StopsDbEnumerator_ShouldMoveToGlobalId()
        {
            var db = new TripsDb();
            var id1 = db.Add("http://irail.be/vehicle/IC725", 
                new Attribute("headsign", "IC725"), new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal"));
            var id2 = db.Add("http://irail.be/vehicle/IC704", 
                new Attribute("headsign", "IC704"), new Attribute("name", "Antwerpen-Centraal - Poperinge"));

            var enumerator = db.GetReader();
            Assert.True(enumerator.MoveTo("http://irail.be/vehicle/IC725"));
            Assert.Equal("http://irail.be/vehicle/IC725", enumerator.GlobalId);
            Assert.Equal(new AttributeCollection(new Attribute("headsign", "IC725"), new Attribute("name", "Gent-Sint-Pieters - Antwerpen-Centraal")), 
                enumerator.Attributes);
            Assert.True(enumerator.MoveTo("http://irail.be/vehicle/IC704"));
            Assert.Equal("http://irail.be/vehicle/IC704", enumerator.GlobalId);
            Assert.Equal(new AttributeCollection(new Attribute("headsign", "IC704"), new Attribute("name", "Antwerpen-Centraal - Poperinge")), 
                enumerator.Attributes);
        }
    }
}