using System;
using Itinero.Transit.IO.OSM.Data.OpeningHours;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM.OpeningHours
{
    public class RdParserTest
    {

        [Fact]
        public void TestDefaults()
        {

            Assert.Equal(42, DefaultRdParsers.Int().ParseFull("42"));
            Assert.Equal(42.0, DefaultRdParsers.Double().ParseFull("42.0"));

            try
            {

                DefaultRdParsers.Int().ParseFull("42CanNotParse");
                Assert.True(false);
            }
            catch(FormatException e)
            {

            }


        }
        
    }
}