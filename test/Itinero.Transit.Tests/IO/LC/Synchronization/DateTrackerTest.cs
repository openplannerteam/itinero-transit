using System;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.IO.LC.Synchronization
{
    public class DateTrackerTest
    {
        [Fact]
        public void AddTimeWindow_4Windows_ExpectsToBeMerged()
        {
            var dt = new DateTracker();

            Assert.Empty(dt.TimeWindows());

            var d = new DateTime(2019, 01, 28, 10, 00, 00);

            dt.AddTimeWindow(d, d.AddMinutes(30));
            Assert.Single(dt.TimeWindows());
            Assert.Equal((d, d.AddMinutes(30)), dt.TimeWindows()[0]);


            dt.AddTimeWindow(d.AddMinutes(15), d.AddMinutes(45));


            Assert.Single(dt.TimeWindows());
            Assert.Equal((d, d.AddMinutes(45)), dt.TimeWindows()[0]);
            dt.AddTimeWindow(d.AddMinutes(-15), d.AddMinutes(45));
            Assert.Single(dt.TimeWindows());
            Assert.Equal((d.AddMinutes(-15), d.AddMinutes(45)), dt.TimeWindows()[0]);


            dt.AddTimeWindow(d.AddMinutes(-60), d.AddMinutes(-30));
            Assert.Equal(2, dt.TimeWindows().Count);


            dt.AddTimeWindow(d.AddMinutes(-30), d.AddMinutes(0));
            Assert.Single(dt.TimeWindows());
            Assert.Equal((d.AddMinutes(-60), d.AddMinutes(45)), dt.TimeWindows()[0]);
        }


        [Fact]
        public void AddTimeWindow_2Windows_ExpectsGap()
        {
            var dt = new DateTracker();
            var d = new DateTime(2019, 01, 28, 10, 00, 00);

            dt.AddTimeWindow(d, d.AddMinutes(30));

            var gaps = dt.CalculateGaps(d.AddMinutes(-15), d.AddMinutes(45));
            Assert.Equal(2, gaps.Count);
            Assert.Equal((d.AddMinutes(-15), d), gaps[0]);
            Assert.Equal((d.AddMinutes(30), d.AddMinutes(45)), gaps[1]);

            dt.AddTimeWindow(d.AddMinutes(15), d.AddMinutes(45));

            gaps = dt.CalculateGaps(d.AddMinutes(-15), d.AddMinutes(45));
            Assert.Single(gaps);
            Assert.Equal((d.AddMinutes(-15), d), gaps[0]);
        }


        [Fact]
        public void CalculateGaps_OneBigWindow_ExpectsNone()
        {
            var dt = new DateTracker();
            var d = DateTime.Today;

            dt.AddTimeWindow(d, d.AddDays(2));

            var gaps = dt.CalculateGaps(d.AddHours(10), d.AddHours(34));
            Assert.Empty(gaps);
        }
        
        [Fact]
        public void CalculateGaps_TwoWindows_ExpectsONeGap()
        {
            var dt = new DateTracker();
            var d = DateTime.Today.Date;

            dt.AddTimeWindow(d, d.AddDays(2));

            var gaps = dt.CalculateGaps(d.AddDays(3), d.AddDays(4));
            Assert.Single(gaps);
            Assert.Equal((d.AddDays(3), d.AddDays(4)), gaps[0]);
        }
        
        
                
        [Fact]
        public void CalcualteGaps_NoWindows_ExpectsBigGap()
        {
            var dt = new DateTracker();
            var d = DateTime.Today.Date;

            var gaps = dt.CalculateGaps(d.AddDays(3), d.AddDays(4));
            Assert.Single(gaps);
            Assert.Equal((d.AddDays(3), d.AddDays(4)), gaps[0]);
        }
    }
}