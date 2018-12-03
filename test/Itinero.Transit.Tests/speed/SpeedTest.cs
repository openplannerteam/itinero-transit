using Itinero.IO.LC;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    /// <summary>
    /// Used to test performance of the algorithms. It's not really the correctness that is tested here
    /// </summary>
    public class SpeedTest : SuperTest
    {
        private Profile<TransferStats> Sncb;

        public SpeedTest(ITestOutputHelper output) : base(output)
        {
            Sncb = Belgium.Sncb(new LocalStorage(ResourcesTest.TestPath),
                new Downloader(caching: true));
        }

        [Fact]
        public void TestEas1()
        {
            var nrOfRuns = 10;
            Tic();
            for (int i = 0; i < nrOfRuns; i++)
            {
                Sncb.CalculateEas(TestEas.Poperinge, TestEas.Vielsalm, ResourcesTest.TestMoment(10, 00),
                    ResourcesTest.TestMoment(20, 00));
            }

            var time = Toc();
            Pr($"Needed {time} millsec ({time/nrOfRuns}/case)");
        }
    }
}