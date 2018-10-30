using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class ResourcesTest
    {
        private readonly ITestOutputHelper _output;

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
            if (Directory.Exists("timetables-for-testing-2018-10-17"))
            {
                Log("Timetables are found");
                return;
            }

            var SourcePath = Path.GetFullPath("../../../testdata/timetables-for-testing-2018-10-17");
            var DestinationPath = Path.GetFullPath("timetables-for-testing-2018-10-17");
            Directory.CreateDirectory(DestinationPath);
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", 
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                Log($"{newPath} --> {newPath.Replace(SourcePath, DestinationPath)}");
                var dest = newPath.Replace(SourcePath, DestinationPath);
                File.Copy(newPath, dest, true);
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