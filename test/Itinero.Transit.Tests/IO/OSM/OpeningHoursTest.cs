using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.OpeningHoursRDParser;
using Xunit;

// ReSharper disable PossibleInvalidOperationException

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class OpeningHoursTest
    {
        [Fact]
        public void ParserBasics()
        {
            var (a, rest0) = OpeningHoursRdParsers.Regex("[a-zA-Z]*").Parse("abc def").Value;
            Assert.Equal("abc", a);
            Assert.Equal(" def", rest0);

            (a, rest0) = OpeningHoursRdParsers.LitCI("open").Parse("Open").Value;
            Assert.Equal("Open", a);
            Assert.Equal("", rest0);

            var ( r, rest1) = OpeningHoursRdParsers.OSMStatus().Parse("open").Value;
            Assert.Equal("<osmstate: >open", r.ToString());
            Assert.Equal("", rest1);

            var test = OpeningHoursRdParsers.TwentyFourSeven().Parse("24/7 open");
            Assert.NotNull(test);
            var (rule, rest) = test.Value;

            Assert.Equal("", rest);
            Assert.Equal("<osmstate: >open", rule.ToString());
        }

        [Fact]
        public void PeriodParserTest()
        {
            var (i, rest0) = OpeningHoursRdParsers.DayOfWeek().Parse("Mo").Value;
            Assert.Equal(0, i);
            Assert.Equal("", rest0);

            var (dayOfWeek, _) = OpeningHoursRdParsers.DayOfWeekRange().Parse("Mo-Fr").Value;
            Assert.True(dayOfWeek[0]);
            Assert.True(dayOfWeek[1]);
            Assert.True(dayOfWeek[2]);
            Assert.True(dayOfWeek[3]);
            Assert.True(dayOfWeek[4]);
            Assert.False(dayOfWeek[5]);
            Assert.False(dayOfWeek[6]);

            var (moy, _) = OpeningHoursRdParsers.MonthOfYearRange().Parse("Jan,Mar,Jun-Sep").Value;
            Assert.True(
                // BEWARE! 0-indexed months
                moy[0] & moy[2] & moy[5] & moy[6] & moy[7] & moy[8]);
            Assert.False(
                moy[1] | moy[3] | moy[4] | moy[9] | moy[10] | moy[11]);
        }


        [Fact]
        public void TestOhAdvanced()
        {
            var oh = "Mo-Th 07:30-22:00 \"a\"; Fr 07:30-24:00 \"b\"; Sa 09:00-24:00 \"c\"; Su 09:00-24:00" // "; Aug Su off"
                .ParseOpeningHoursRule("Europe/Brussels");


            Assert.Equal("\"b\"", oh.StateAt(new DateTime(2019, 05, 17, 12, 00, 00)));
        }


        [Fact]
        public void TestTimeZones()
        {
            var oh = "10:00-20:00".ParseOpeningHoursRule("Europe/Brussels");

            var dt = new DateTime(2019, 05, 14, 11, 00, 00, DateTimeKind.Unspecified);
            Assert.Equal("", oh.StateAt(dt));

            dt = new DateTime(2019, 05, 14, 09, 00, 00, DateTimeKind.Utc); // Same time, but 11:00 in brussels
            Assert.Equal("", oh.StateAt(dt));


            dt = new DateTime(2019, 05, 14, 19, 00, 00, DateTimeKind.Utc); // 21:00 in brussels -> Closed
            Assert.Equal("closed", oh.StateAt(dt, "closed"));
        }

        [Fact]
        public void TestOh()
        {
            var weekday = 
                "mo-fr 09:00-12:00,13:00-17:00 open".ParseOpeningHoursRule("europe/Brussels");

            // Mo
            var state = weekday.StateAt(new DateTime(2019, 04, 22, 16, 00, 00), "closed");
            Assert.Equal("open", state);

            state = weekday.StateAt(new DateTime(2019, 04, 22, 17, 01, 00), "closed");
            Assert.Equal("closed", state);

            state = weekday.StateAt(new DateTime(2019, 04, 22, 12, 30, 00), "closed");
            Assert.Equal("closed", state);


            state = weekday.StateAt(new DateTime(2019, 04, 22, 11, 30, 00), "closed");
            Assert.Equal("open", state);
            // Tue
            state = weekday.StateAt(new DateTime(2019, 04, 23, 16, 00, 00), "closed");
            Assert.Equal("open", state);

            // We
            state = weekday.StateAt(new DateTime(2019, 04, 24, 16, 00, 00), "closed");
            Assert.Equal("open", state);

            // Th
            state = weekday.StateAt(new DateTime(2019, 04, 25, 16, 00, 00), "closed");
            Assert.Equal("open", state);

            // Fr
            state = weekday.StateAt(new DateTime(2019, 04, 26, 16, 00, 00), "closed");
            Assert.Equal("open", state);
            // Sa
            state = weekday.StateAt(new DateTime(2019, 04, 27, 16, 00, 00), "closed");
            Assert.Equal("closed", state);
            // Su
            state = weekday.StateAt(new DateTime(2019, 04, 28, 16, 00, 00), "closed");
            Assert.Equal("closed", state);
        }
    }
}