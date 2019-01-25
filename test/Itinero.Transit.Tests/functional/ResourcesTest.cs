using System;
using System.IO;
using System.Linq;
using System.Net;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.Utils;
using Itinero.Transit.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.IO.LC.Tests
{
    public class ResourcesTest : SuperTest
    {
        public const string TestPath = "timetables-for-testing-2018-12-12";

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly DateTime TestDay = new DateTime(2018, 12, 12, 00, 00, 00);

        public ResourcesTest(ITestOutputHelper output) : base(output)
        {
        }

        public static DateTime TestMoment(int hours, int minutes, int seconds = 0)
        {
            return TestDay.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
        }

        [Fact]
        public void FixCache()
        {
            if (Directory.Exists(TestPath + "/SNCB/timetables") &&
                Directory.EnumerateFiles(TestPath + "/SNCB/timetables").Count() > 300)
            {
                return;
            }

            try
            {
                var sncb = Belgium.Sncb();

                var deLijn = Belgium.DeLijn();
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.InnerException?.Message);
                Log(e.InnerException?.InnerException?.Message);

                throw;
            }
        }

    }
}