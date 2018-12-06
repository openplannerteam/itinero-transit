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
            Assert.True(DateTimeExtensions.FromUnixTime(now.ToUnixTime())<
                        DateTimeExtensions.FromUnixTime(nextYear.ToUnixTime()));
            Assert.True(nextYear == DateTimeExtensions.FromUnixTime(nextYear.ToUnixTime()));
            
            
        }
        
    }
}