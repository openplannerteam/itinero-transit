using Itinero_Transit;
using Itinero_Transit.CSA;
using Itinero_Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class TestLinkedObject
    {
        private readonly ITestOutputHelper _output;

        public TestLinkedObject(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestLinkedConnectionDownloader()
        {
            Downloader.AlwaysReturn = singleConnection;
            var result = Downloader.Download(Program.IRail);
            Assert.Equal(result, singleConnection);
            log("Downloaded result");
            log(Downloader.AsJson(result)["@graph"][0].ToString());
        }

        [Fact]
        public void TestJSONParsing()
        {
            Downloader.AlwaysReturn = singleConnection;
            var tt = new TimeTable(Downloader.DownloadJson(Program.IRail));
            log(tt.ToString());
            const string exp =
                "Timetable with 2 connections; ID: connections Next: connections Prev: connections\n  Connection http://irail.be/stations/NMBS/008822137 2018-09-19 13:30:00 --> http://irail.be/stations/NMBS/008893559 2018-09-19 13:51:00\n    Direction Gand-Saint-Pierre (http://irail.be/connections/8822137/20180919/IC4136)\n  Connection http://irail.be/stations/NMBS/008863008 2018-09-19 13:30:00 --> http://irail.be/stations/NMBS/008863461 2018-09-19 13:35:00\n    Direction Liege-Guillemins (http://irail.be/connections/8863008/20180919/L4965)";
            Assert.Equal(tt.ToString(), exp);
        }


        private void log(string s)
        {
            _output.WriteLine(s);
        }

        public static readonly string singleConnection =
            "{\"@context\":{\"xsd\":\"http://www.w3.org/2001/XMLSchema#\",\"lc\":\"http://semweb.mmlab.be/ns/linkedconnections#\",\"hydra\":\"http://www.w3.org/ns/hydra/core#\",\"gtfs\":\"http://vocab.gtfs.org/terms#\",\"Connection\":\"lc:Connection\",\"arrivalTime\":{\"@id\":\"lc:arrivalTime\",\"@type\":\"xsd:dateTime\"},\"departureTime\":{\"@id\":\"lc:departureTime\",\"@type\":\"xsd:dateTime\"},\"arrivalStop\":{\"@type\":\"@id\",\"@id\":\"lc:arrivalStop\"},\"departureStop\":{\"@type\":\"@id\",\"@id\":\"lc:departureStop\"},\"departureDelay\":{\"@id\":\"lc:departureDelay\",\"@type\":\"xsd:integer\"},\"arrivalDelay\":{\"@id\":\"lc:arrivalDelay\",\"@type\":\"xsd:integer\"},\"direction\":{\"@id\":\"gtfs:headsign\",\"@type\":\"xsd:string\"},\"gtfs:trip\":{\"@type\":\"@id\"},\"gtfs:route\":{\"@type\":\"@id\"},\"gtfs:pickupType\":{\"@type\":\"@id\"},\"gtfs:dropOffType\":{\"@type\":\"@id\"},\"gtfs:Regular\":{\"@type\":\"@id\"},\"gtfs:NotAvailable\":{\"@type\":\"@id\"},\"hydra:next\":{\"@type\":\"@id\"},\"hydra:previous\":{\"@type\":\"@id\"},\"hydra:property\":{\"@type\":\"@id\"},\"hydra:variableRepresentation\":{\"@type\":\"@id\"}},\"@id\":\"https://graph.irail.be/sncb/connections?departureTime=2018-09-19T13:30:00.000Z\",\"@type\":\"hydra:PagedCollection\",\"hydra:next\":\"https://graph.irail.be/sncb/connections?departureTime=2018-09-19T13:33:00.000Z\",\"hydra:previous\":\"https://graph.irail.be/sncb/connections?departureTime=2018-09-19T13:27:00.000Z\",\"hydra:search\":{\"@type\":\"hydra:IriTemplate\",\"hydra:template\":\"https://graph.irail.be/sncb/connections{?departureTime}\",\"hydra:variableRepresentation\":\"hydra:BasicRepresentation\",\"hydra:mapping\":{\"@type\":\"IriTemplateMapping\",\"hydra:variable\":\"departureTime\",\"hydra:required\":true,\"hydra:property\":\"lc:departureTimeQuery\"}}," +
            "\"@graph\":[{\"@id\":\"http://irail.be/connections/8822137/20180919/IC4136\",\"@type\":\"Connection\",\"departureStop\":\"http://irail.be/stations/NMBS/008822137\",\"arrivalStop\":\"http://irail.be/stations/NMBS/008893559\",\"departureTime\":\"2018-09-19T13:30:00.000Z\",\"arrivalTime\":\"2018-09-19T13:51:00.000Z\",\"departureDelay\":60,\"arrivalDelay\":60,\"direction\":\"Gand-Saint-Pierre\",\"gtfs:trip\":\"http://irail.be/vehicle/IC4136/20180919\",\"gtfs:route\":\"http://irail.be/vehicle/IC4136\"},{\"@id\":\"http://irail.be/connections/8863008/20180919/L4965\",\"@type\":\"Connection\",\"departureStop\":\"http://irail.be/stations/NMBS/008863008\",\"arrivalStop\":\"http://irail.be/stations/NMBS/008863461\",\"departureTime\":\"2018-09-19T13:30:00.000Z\",\"arrivalTime\":\"2018-09-19T13:35:00.000Z\",\"departureDelay\":180,\"arrivalDelay\":0,\"direction\":\"Liege-Guillemins\",\"gtfs:trip\":\"http://irail.be/vehicle/L4965/20180919\",\"gtfs:route\":\"http://irail.be/vehicle/L4965\"}]}";
    }
}