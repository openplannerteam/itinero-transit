using System;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.IO.GTFS;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class GTFSExtensionsTests
    {
        [Fact]
        public void GTFSExtensions_GetDatePatterns_NoCalendars_PatternsShouldBeEmpty()
        {
            var feed = new GTFSFeed();

            var patterns = feed.GetDatePatterns();
            
            Assert.Empty(patterns);
        }
        
        [Fact]
        public void GTFSExtensions_GetDatePatterns_OneWeekPattern_PatternsShouldHaveSamePattern()
        {
            var feed = new GTFSFeed();
            feed.Calendars.Add(new Calendar()
            {
                ServiceId = "service1",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
            });

            var patterns = feed.GetDatePatterns();
            
            Assert.Single(patterns);
            var patternPair = patterns.First();
            Assert.Equal("service1", patternPair.Key);
            var pattern = patternPair.Value;
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 27)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 28)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 29)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 30)));
            Assert.True(pattern.IsActiveOn(new DateTime(2020, 01, 31)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 01)));
            Assert.False(pattern.IsActiveOn(new DateTime(2020, 02, 02)));
        }
        
        [Fact]
        public void GTFSExtensions_GetDatePatterns_OneWeekPatternAndOneException_PatternsShouldHaveSamePattern()
        {
            var feed = new GTFSFeed();
            feed.Calendars.Add(new Calendar()
            {
                ServiceId = "service1",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
            });
            feed.CalendarDates.Add(new CalendarDate()
            {
                ServiceId = "service1",
                Date = new DateTime(2020, 02, 02),
                ExceptionType = ExceptionType.Added
            });

            var patterns = feed.GetDatePatterns();
            
            Assert.Single(patterns);
            var patternPair = patterns.First();
            Assert.Equal("service1", patternPair.Key);
            var pattern = patternPair.Value;
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