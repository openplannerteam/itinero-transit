using Itinero.Transit.IO.LC.Data;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Itinero.Transit.Tests.IO.LC
{
    public class ParseJsonLdTest
    {
        private const string _testString =
            "{\"@id\":\"http://irail.be/connections/8814001/20190617/IC3115\"," +
            "\"@type\":[\"http://semweb.mmlab.be/ns/linkedconnections#CancelledConnection\"]," +
            "\"http://semweb.mmlab.be/ns/linkedconnections#departureStop\":" +
            "{\"@id\":\"http://irail.be/stations/NMBS/008814001\"}," +
            "\"http://semweb.mmlab.be/ns/linkedconnections#arrivalStop\":" +
            "{\"@id\":\"http://irail.be/stations/NMBS/008813037\"}," +
            "\"http://semweb.mmlab.be/ns/linkedconnections#departureTime\":" +
            "    {\"@value\": \"2019-06-17T13:57:00.000Z\"}," +
            "\"http://semweb.mmlab.be/ns/linkedconnections#arrivalTime\":" +
            "    {\"@value\": \"2019-06-17T13:59:00.000Z\"}," +
            "\"http://vocab.gtfs.org/terms#trip\":" +
            "    {\"@id\": \"http://irail.be/vehicle/IC3115/20190617\"}," +
            "\"http://vocab.gtfs.org/terms#route\":" +
            "    {\"@id\": \"http://irail.be/vehicle/IC3115\"}," +
            "\"http://vocab.gtfs.org/terms#headsign\":" +
            "    {\"@value\": \"Anvers-Central\"}," +
            "\"http://vocab.gtfs.org/terms#pickupType\":" +
            "    {\"@id\": \"http://vocab.gtfs.org/terms#Regular\"}," +
            "\"http://vocab.gtfs.org/terms#dropOffType\":" +
            "    {\"@id\": \"http://vocab.gtfs.org/terms#NotAvailable\"}" +
            "}";


        [Fact]
        public void ConnectionParse_TestString_ExpectsCancelledConnection()
        {
            var conn = new Connection(JObject.Parse(_testString));
            Assert.True(conn.IsCancelled);
            
            
        }
    }
}