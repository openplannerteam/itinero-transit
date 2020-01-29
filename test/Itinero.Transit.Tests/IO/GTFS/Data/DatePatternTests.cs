using System;
using Itinero.Transit.IO.GTFS.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS.Data
{
    public class DatePatternTests
    {
        [Fact]
        public void DatePattern_NoWeekPattern_IsActiveOn_ShouldReturnFalse()
        {
            var pattern = new DatePattern();
            
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 26)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
        }
        
        [Fact]
        public void DatePattern_NoWeekPattern_PositiveException_IsActiveOn_ShouldReturnTrueOnlyOnException()
        {
            var pattern = new DatePattern();
            pattern.AddException(new DateTime(2020, 01, 30), true);
            
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 26)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
        }
        
        [Fact]
        public void DatePattern_NoWeekPattern_NegativeException_IsActiveOn_ShouldReturnFalse()
        {
            var pattern = new DatePattern();
            pattern.AddException(new DateTime(2020, 01, 30), false);
            
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 26)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
        }
        
        [Fact]
        public void DatePattern_WeekdaysPattern_IsActiveOn_ShouldReturnTrueOnWeekdays()
        {
            var pattern = new DatePattern(new WeekPattern()
            {
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
            });
            
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 02)));
        }
        
        [Fact]
        public void DatePattern_WeekdaysPattern_OneWeekendException_IsActiveOn_ShouldReturnTrueOnWeekdaysAndOnException()
        {
            var pattern = new DatePattern(new WeekPattern()
            {
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
            });
            pattern.AddException(new DateTime(2020, 02, 02), true);
            
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 01, 26)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 02, 02)));
        }
    }
}