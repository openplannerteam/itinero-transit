using System;
using JsonLD.Core;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests
{
    public class TestCacheCow
    {
        private readonly ITestOutputHelper _output;

        public TestCacheCow(ITestOutputHelper output)
        {
            _output = output;
        }



        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }


        [Fact]
        public void TestCache()
        {
            
           Assert.True(true);
            
            
            
        }
        
        
    }
}