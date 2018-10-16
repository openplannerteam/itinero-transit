using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class TestSncbLocations
    {
        private readonly ITestOutputHelper _output;

        public TestSncbLocations(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestCloseLocations()
        {
            var lat = 51.21576f;
            var lon = 3.22048f;

            var uri = new Uri("http://irail.be/stations");
            var locs = new LocationsDump(uri);


            var loader = new Downloader(caching: false);
            var proc = new JsonLdProcessor(loader, uri);

            locs.Download(proc);

            var found = (HashSet<Uri>) locs.GetLocationsCloseTo(lat, lon, 5000);

            Assert.True(found.Contains(new Uri("http://irail.be/stations/NMBS/008891009")));
            Assert.True(found.Contains(new Uri("http://irail.be/stations/NMBS/008891033")));
            Assert.Equal(2, found.Count());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}