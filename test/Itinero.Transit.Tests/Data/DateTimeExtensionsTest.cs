using System;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class DateTimeExtensionsTest
    {
        [Fact]
        public void TestMaxDateTime()
        {
            var now = new DateTime(2105, 1, 1);
            var nextYear = now.AddYears(1);
            
            Assert.True(now.ToUnixTime() < nextYear.ToUnixTime());
            Assert.True(now.ToUnixTime().FromUnixTime()<
                        nextYear.ToUnixTime().FromUnixTime());
            Assert.True(nextYear == nextYear.ToUnixTime().FromUnixTime());
        }
        
    }
}