using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit.CSA.ConnectionProviders;
using Itinero.Transit.CSA.Data;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class ResourcesTest
    {
        private readonly ITestOutputHelper _output;
        public static readonly string TestPath = "timetables-for-testing-2018-11-26";
        public static readonly DateTime TestDay = new DateTime(2018,11,26,00,00,00);

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
            _output.WriteLine(s);
        }

        [Fact]
        public void FixCache()
        {

            if (Directory.Exists(TestPath+"/timetables") && 
                Directory.EnumerateFiles(TestPath + "/timetables").Count() > 100)
            {
                return;
            }

            var sncb = Sncb.Profile(TestPath, "belgium.routerdb");
            try
            {
                sncb.DownloadDay(TestDay);

            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.InnerException?.Message);
                Log(e.InnerException?.InnerException?.Message);
            }

            

        }

        /// <summary>
        ///  This test downloads belgium.osm.pbf and builds the router database (if it doesn't exist yet)
        /// </summary>
        [Fact]
        public void FixRouterDB()
        {
            if (File.Exists("belgium.routerdb"))
            {
                Log("Found the routerdb already.");
                return;
            }

            
            Log("Downloading routerdb...");
            var geofabrikBE = new Uri("http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf");

            var fileReq = (HttpWebRequest) HttpWebRequest.Create(geofabrikBE);
            var fileResp = (HttpWebResponse) fileReq.GetResponse();
            using (var httpstream = fileResp.GetResponseStream())
            {
                var fileStream = File.Create("belgium.osm.pbf");
                httpstream.CopyTo(fileStream);
                fileStream.Close();
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