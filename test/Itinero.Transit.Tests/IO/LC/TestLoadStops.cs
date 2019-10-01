using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.Data;
using Itinero.Transit.IO.LC.Utils;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Itinero.Transit.Tests.IO.LC
{
    public class TestLoadStops
    {
        [Fact]
        public void LoadAllStops_NmbsTestData_ExpectsAllStops()
        {
            var testData = File.ReadAllText("test-data/stops.json");
            var downloader = new Downloader {AlwaysReturn = testData};

            var expectedCount = JObject.Parse(testData)["@graph"].Count();
            Assert.Equal(2628, expectedCount);

            var allLocations = new LocationProvider(new Uri("http://example.com"));
            allLocations.Download(new JsonLdProcessor(downloader, new Uri("https://graph.irail.be/sncb/")));
            var list = allLocations.Locations;
            Assert.Equal(expectedCount, list.Count);


            var tdb = new TransitDb(0);
            var writer = tdb.GetWriter();
        }
    }
}