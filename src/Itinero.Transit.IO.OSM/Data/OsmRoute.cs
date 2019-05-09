using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Xml;
using GeoAPI.Geometries;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Loads a PTv2-route relation from OSM, and adds it to a transitDB (for a given timerange)
    /// </summary>
    internal class OsmRoute
    {
        public List<(string, Coordinate, TagsCollectionBase)> StopPositions;
        public bool RoundTrip;
        public TimeSpan Duration;
        public TimeSpan Interval;
        public IOpeningHoursRule OpeningTimes;
        public long Id;

        public OsmRoute(CompleteRelation relation)
        {
            var ts = relation.Tags;
            Id = relation.Id;
            RoundTrip = ts.ContainsKey("roundtrip") && ts["roundtrip"].Equals("yes");

            Duration = TimeSpan.ParseExact(ts["duration"], "hh\\:mm\\:ss", null);
            Interval = TimeSpan.ParseExact(ts["interval"], "hh\\:mm\\:ss", null);

            if (ts.ContainsKey("opening_hours"))
            {
                OpeningTimes = OpeningHours.Parse(ts["opening_hours"]) ?? new TwentyFourSeven();
            }
            else
            {
                OpeningTimes = new TwentyFourSeven();
            }

            StopPositions = new List<(string, Coordinate, TagsCollectionBase)>();

            foreach (var member in relation.Members)
            {
                var el = member.Member;
                if (member.Role.Equals("stop") && el is Node node)
                {
                    if (node.Latitude == null || node.Longitude == null)
                    {
                        throw new ArgumentNullException();
                    }

                    var coor = new Coordinate((double) node.Latitude, (double) node.Longitude);
                    var nodeId = $"https://www.openstreetmap.org/node/{node.Id}";
                    StopPositions.Add((nodeId, coor, el.Tags));
                }
            }
        }


        public static List<OsmRoute> LoadFromFile(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                return OsmRouteFromStream(fileStream);
            }
        }

        public static List<OsmRoute> LoadFromOsm(long relationId)
        {
            return LoadFromUrl(new Uri($"https://www.openstreetmap.org/api/0.6/relation/{relationId}/full"));
        }

        public static List<OsmRoute> LoadFromUrl(Uri path)
        {
            var httpClient = new HttpClient();

            var response = httpClient.GetAsync(path).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new WebException($"Could not download {path}");
            }


            using (var fileStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
            {
                try
                {
                    return OsmRouteFromStream(fileStream);
                }
                catch (XmlException e)
                {
                    throw new XmlException(
                        "Trying to load the relation failed. You probably fed a webpage to this method instead of an OSM-relation. Try osm.org/api/0.6/relation/\\{relationId\\}/full instead - or our builtin method",
                        e);
                }
            }
        }


        private static List<OsmRoute> OsmRouteFromStream(Stream fileStream)
        {
            var routes = new List<OsmRoute>();
            var source = new XmlOsmStreamSource(fileStream);
            var stream = source.ToComplete();


            while (stream.MoveNext())
            {
                var cur = stream.Current();

                if (!(cur is CompleteRelation rel))
                {
                    continue;
                }

                if (cur.Tags.ContainsKey("type") && cur.Tags["type"].Equals("route"))
                {
                    routes.Add(new OsmRoute(rel));
                }
            }

            return routes;
        }
    }
}