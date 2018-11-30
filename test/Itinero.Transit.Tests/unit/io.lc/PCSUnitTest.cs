using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit.Tests.unit.io.lc
{
    public class PCSUnitTest : SuperTest
    {
        public PCSUnitTest(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public void TestTheoretical()
        {
            Log("Starting");
            var test = new TestProfile(new DateTime(2018, 11, 26));
            var prof = test.CreateTestProfile();
            prof.IntermodalStopSearchRadius = 10000;

            var pcs = new ProfiledConnectionScan<TransferStats>(TestProfile.A, TestProfile.D,
                test.Moment(17, 00), test.Moment(19, 01), prof
            );


            var journeys = pcs.CalculateJourneys();


            var found = 0;
            var stats = "";
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(prof));
                    stats += $"{key}: {journey.Stats}\n";
                    Assert.Equal(2, (int) journey.Stats.TravelTime.TotalHours);
                }

                // ReSharper disable once PossibleMultipleEnumeration
                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
        }
    }
}