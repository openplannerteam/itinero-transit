using System;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit.Tests.Data
{
    public class JourneyTest : SuperTest
    {
        public JourneyTest(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void TestSimpleJourney()
        {
            var time = new DateTime(2018, 12, 04, 16, 20, 00).ToUnixTime();
            var j = new Journey<TransferStats>(0, time,
                new TransferStats());
            
            
        }
    }
}