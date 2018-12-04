using System;
using OsmSharp.IO.PBF;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class DateTimeExtensionsTest
    {


        [Fact]
        public void TestMaxDateTime()
        {

            var zero = new DateTime(2006, 2, 7).ToUnixTime();
            var pointInTime = zero;
            var newDate = DateTimeExtensions.FromUnixTime(pointInTime + uint.MaxValue);
            var point2 = newDate.ToUnixTime();
            Assert.False(pointInTime == point2);
            Assert.True(point2 > pointInTime);

        }
        
    }
}