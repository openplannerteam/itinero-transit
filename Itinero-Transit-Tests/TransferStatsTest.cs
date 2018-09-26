using System;
using Itinero_Transit.CSA;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class TransferStatsTest
    {
        private readonly ITestOutputHelper _output;

        public TransferStatsTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestComparePareto()
        {
            var tenOClock = new DateTime(2018, 09, 24, 10, 00, 00);
            // Takes one hour but no transfer
            var t1 = new TransferStats(0, tenOClock,
                new DateTime(2018, 09, 24, 11, 00, 00));


            // Takes 45min, but one transfer
            var t2 = new TransferStats(1, tenOClock,
                new DateTime(2018, 09, 24, 10, 45, 00));

            // Superior in all senses: takes no transfers and just 30 min
            var t3 = new TransferStats(0, tenOClock,
                new DateTime(2018, 09, 24, 10, 30, 00));

            IStatsComparator<TransferStats> compare = TransferStats.ProfileCompare;
            
            Assert.Equal(0, compare.ADominatesB(t1, t1));
            Assert.Equal(0, compare.ADominatesB(t2, t2));
            Assert.Equal(0, compare.ADominatesB(t3, t3));
            
            Assert.Equal(1, compare.ADominatesB(t1, t3));
            Assert.Equal(int.MaxValue, compare.ADominatesB(t1, t2));
            Assert.Equal(1, compare.ADominatesB(t2, t3));


            Assert.Equal(-1, compare.ADominatesB(t3, t1));
            Assert.Equal(int.MaxValue, compare.ADominatesB(t2, t1));
            Assert.Equal(-1, compare.ADominatesB(t3, t2));
            
            
            compare = TransferStats.MinimizeTransfers;
            
            Assert.Equal(0, compare.ADominatesB(t1, t1));
            Assert.Equal(0, compare.ADominatesB(t2, t2));
            Assert.Equal(0, compare.ADominatesB(t3, t3));
            
            Assert.Equal(0, compare.ADominatesB(t1, t3));
            Assert.Equal(-1, compare.ADominatesB(t1, t2));
            Assert.Equal(1, compare.ADominatesB(t2, t3));


            Assert.Equal(0, compare.ADominatesB(t3, t1));
            Assert.Equal(1, compare.ADominatesB(t2, t1));
            Assert.Equal(-1, compare.ADominatesB(t3, t2));
            
                        
            compare = TransferStats.MinimizeTransfersFirst;
            
            Assert.Equal(0, compare.ADominatesB(t1, t1));
            Assert.Equal(0, compare.ADominatesB(t2, t2));
            Assert.Equal(0, compare.ADominatesB(t3, t3));
            
            Assert.Equal(1, compare.ADominatesB(t1, t3));
            Assert.Equal(-1, compare.ADominatesB(t1, t2));
            Assert.Equal(1, compare.ADominatesB(t2, t3));


            Assert.Equal(-1, compare.ADominatesB(t3, t1));
            Assert.Equal(1, compare.ADominatesB(t2, t1));
            Assert.Equal(-1, compare.ADominatesB(t3, t2));
            
            compare = TransferStats.MinimizeTravelTimes;
            
            Assert.Equal(0, compare.ADominatesB(t1, t1));
            Assert.Equal(0, compare.ADominatesB(t2, t2));
            Assert.Equal(0, compare.ADominatesB(t3, t3));
            
            Assert.Equal(1, compare.ADominatesB(t1, t3));
            Assert.Equal(1, compare.ADominatesB(t1, t2));
            Assert.Equal(1, compare.ADominatesB(t2, t3));


            Assert.Equal(-1, compare.ADominatesB(t3, t1));
            Assert.Equal(-1, compare.ADominatesB(t2, t1));
            Assert.Equal(-1, compare.ADominatesB(t3, t2));


        }
        
        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}