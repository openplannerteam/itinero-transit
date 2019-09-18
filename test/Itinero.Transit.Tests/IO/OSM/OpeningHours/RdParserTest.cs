using System;
using Itinero.Transit.IO.OSM.Data.Parser;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM.OpeningHours
{
    public class RdParserTest
    {

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
            catch(FormatException)
            {

            }


        }
        
    }
}