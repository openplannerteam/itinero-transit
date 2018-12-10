using System;
using System.IO;
using System.Linq;
using System.Net;
using Itinero.IO.LC;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit;
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
        public void TestSearchTimeTable()
        {
            var storage = new LocalStorage(TestPath + "/SNCB/timetables");
            Assert.True(storage.KnownKeys().Count > 200);

            var prov = Belgium.Sncb(new LocalStorage(TestPath));
            var tt0 =
                ((LocallyCachedConnectionsProvider) (prov.ConnectionsProvider)).TimeTableContaining(TestMoment(10, 01));
            Assert.NotNull(tt0);
            var tt =
                ((LocallyCachedConnectionsProvider) (prov.ConnectionsProvider)).TimeTableContaining(TestMoment(10, 00));
            Assert.NotNull(tt);
            Assert.Equal("https://graph.irail.be/sncb/connections?departureTime=2018-12-12T10:00:00.000Z",
                tt.Id().ToString());
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
                //    Directory.Delete(TestPath, recursive: true);

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

        [Fact]
        public void DownloadPast()
        {
            var sncb = Belgium.Sncb(new LocalStorage("SNCB-Past"));
            try
            {
                var tt = sncb.ConnectionsProvider.GetTimeTable(new DateTime(2018, 1, 1));
                throw new Exception("Downloading this much in the past should have failed");
            }
            catch(ArgumentException e)
            {
                // Indeed, should error
                Assert.True(true);
            }
            catch(IOException e)
            {
                // Indeed, should error
                Assert.True(true);
            }
        }
    }
}