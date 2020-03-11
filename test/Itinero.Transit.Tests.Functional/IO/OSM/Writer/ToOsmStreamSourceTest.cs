using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Writer;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.Transit.Tests.Functional.IO.OSM.Writer
{
    public static class ToOsmStreamSourceTest
    {
        public static void Test(TransitDb db)
        {
            var nextId = 0L;

            var osmStreamSource = db.Latest.ToOsmStreamSource(_ =>
            {
                nextId--;
                return nextId;
            });

            using var stream = File.Create("test.osm");
            var osmStreamTarget = new OsmSharp.Streams.XmlOsmStreamTarget(stream);
            osmStreamTarget.RegisterSource(osmStreamSource);
            osmStreamTarget.Pull();
            osmStreamTarget.Flush();
        }
    }
}