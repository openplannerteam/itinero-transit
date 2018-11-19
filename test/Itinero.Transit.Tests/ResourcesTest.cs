using System;
using System.IO;
using System.Linq;
using System.Net;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class ResourcesTest
    {
        private readonly ITestOutputHelper _output;
        public const string TestPath = "timetables-for-testing-2018-11-26";

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly DateTime TestDay = new DateTime(2018, 11, 26, 00, 00, 00);

        public static DateTime TestMoment(int hours, int minutes, int seconds = 0)
        {
            return TestDay.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
        }

        public ResourcesTest(ITestOutputHelper output)
        {
            _output = output;
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            if (s == null)
            {
                return;
            }
            _output.WriteLine(s);
        }

        [Fact]
        public void FixCache()
        {
            if (Directory.Exists(TestPath + "/SNCB/timetables") &&
                Directory.EnumerateFiles(TestPath + "/SNCB/timetables").Count() > 100)
            {
                return;
            }

            try
            {
                var st = new LocalStorage(TestPath);
                var sncb = Belgium.Sncb(st);
                sncb.DownloadDay(TestDay);

                var deLijn = Belgium.DeLijn(st);
                deLijn.DownloadDay(TestDay);
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.InnerException?.Message);
                Log(e.InnerException?.InnerException?.Message);

                // NUKE THE CACHE!
                Directory.Delete(TestPath, recursive: true);

                throw;
            }
        }

        /// <summary>
        ///  This test downloads belgium.osm.pbf and builds the router database (if it doesn't exist yet)
        /// </summary>
        [Fact]
        public void FixRouterDb()
        {
            if (File.Exists("belgium.routerdb"))
            {
                Log("Found the routerdb already.");
                return;
            }


            Log("Downloading routerdb...");
            var itineroDownloadsBe = new Uri("http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf");

            var fileReq = (HttpWebRequest) WebRequest.Create(itineroDownloadsBe);
            var fileResp = (HttpWebResponse) fileReq.GetResponse();
            using (var httpStream = fileResp.GetResponseStream())
            {
                using (var fileStream = File.Create("belgium.osm.pbf"))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    httpStream.CopyTo(fileStream);
                    fileStream.Close();
                }
            }

            using (var stream = File.OpenRead("belgium.osm.pbf"))
            {
                var routerDb = new RouterDb();
                Log("Stream successfully opened...");
                routerDb.LoadOsmData(stream, Vehicle.Pedestrian);
                Log("Serializing...");
                using (var outStream = new FileInfo("belgium.routerdb").Open(FileMode.Create))
                {
                    routerDb.Serialize(outStream);
                    Log("DONE!");
                }
            }
        }
    }
}