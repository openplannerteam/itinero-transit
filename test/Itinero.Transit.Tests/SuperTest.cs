using System;
using Xunit.Abstractions;

namespace Itinero.Transit.Tests
{
    public class SuperTest
    {
        private readonly ITestOutputHelper _output;
        private DateTime? startTime;

        public SuperTest(ITestOutputHelper output)
        {
            
            _output = output;
        }

        protected void Tic()
        {
            startTime = DateTime.Now;
        }

        protected double Toc()
        {
            var endTime = DateTime.Now;
            var result = (endTime - startTime)?.TotalMilliseconds;
            startTime = null;
            return (double) result;
        }

        protected void Pr(string s)
        {
            Log(s);
        }
        
        
        protected void Log(string s)
        {
            if (s == null)
            {
                _output.WriteLine("Got a null value to log");
                return;
            }
            _output.WriteLine(s);
        }
    }
}