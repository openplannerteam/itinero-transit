using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
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

            var allLocations = new LocationFragment(new Uri("http://example.com"));
            allLocations.Download(new JsonLdProcessor(downloader, new Uri("https://graph.irail.be/sncb/")));
            var list = allLocations.Locations;
            Assert.Equal(expectedCount, list.Count);

            var transitDb = new TransitDb(0);
            var wr = transitDb.GetWriter();
            wr.AddAllLocations(allLocations);
            wr.Close();

            var stops = transitDb.Latest.StopsDb.GetReader();
            stops.Reset();
            var i = 0;
            while (stops.MoveNext())
            {
                i++;
            }

            Assert.Equal(expectedCount, i);



        }
    }
}