using System;
using Itinero.Transit.IO.OSM.Data.Parser;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM.OpeningHours
{
    public class RdParserTest
    {
        [Fact]
        public void ParseDoubleAsLong_Numbers_ExpectsSameNumber()
        {
            var inputs = new[] {"1.0", "10.0", "1.123", "1.01", "1.10", "1.001", "100.001", "5.1234"};
            var expecteds = new[] {1000L, 10000, 1123, 1010, 1100, 1001, 100001, 5123};

            for (var i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];
                var expected = expecteds[i];
                var actual = DefaultRdParsers.DoubleAsLong(1000).ParseFull(input);
                Assert.Equal(actual, expected);
            }
        }

        [Fact]
        public void Parse_Numbers_ExpectsNumber()
        {
            Assert.Equal(42, DefaultRdParsers.Int().ParseFull("42"));
            Assert.Equal(42.0, DefaultRdParsers.Double().ParseFull("42.0"));

            try
            {
                DefaultRdParsers.Int().ParseFull("42CanNotParse");
                Assert.True(false);
            }
            catch (FormatException)
            {
            }
        }
    }
}