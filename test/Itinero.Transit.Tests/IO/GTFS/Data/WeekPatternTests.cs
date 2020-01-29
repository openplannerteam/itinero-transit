using GTFS.Entities;
using Itinero.Transit.IO.GTFS.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS.Data
{
    public class WeekPatternTests
    {
        [Fact]
        public void WeekPattern_From_NoCalendar_ShouldReturnNull()
        {
            Assert.Null(WeekPattern.From(null));
        }

        [Fact]
        public void WeekPattern_From_WeekdaysCalendar_ShouldReturnWeekdays()
        {
            var pattern = WeekPattern.From(new Calendar()
            {
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
            });
            
            Assert.NotNull(pattern);
            Assert.True(pattern.Value.Monday);
            Assert.True(pattern.Value.Tuesday);
            Assert.True(pattern.Value.Wednesday);
            Assert.True(pattern.Value.Thursday);
            Assert.True(pattern.Value.Friday);
            Assert.False(pattern.Value.Saturday);
            Assert.False(pattern.Value.Sunday);
        }
    }
}