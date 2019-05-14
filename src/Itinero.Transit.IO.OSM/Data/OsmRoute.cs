using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Xml;
using GeoAPI.Geometries;
using GeoTimeZone;
using Itinero.Transit.Logging;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Loads a PTv2-route relation from OSM, and adds it to a transitDB (for a given time range)
    /// </summary>
    internal class OsmRoute
    {
        public readonly List<(string, Coordinate, TagsCollectionBase)> StopPositions;
        public readonly bool RoundTrip;
        public TimeSpan Duration;
        public TimeSpan Interval;
        public readonly IOpeningHoursRule OpeningTimes;
        public readonly long Id;
        public readonly string Name;

        private OsmRoute(CompleteRelation relation)
        {
            var ts = relation.Tags;
            try
            {

                Name = relation.Tags.GetValue("name");
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
            Id = relation.Id;
            RoundTrip = ts.ContainsKey("roundtrip") && ts["roundtrip"].Equals("yes");

            Duration = TimeSpan.ParseExact(ts["duration"], "hh\\:mm\\:ss", null);
            Interval = TimeSpan.ParseExact(ts["interval"], "hh\\:mm\\:ss", null);
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

                    var coordinate = new Coordinate((double) node.Latitude, (double) node.Longitude);
                    var nodeId = $"https://www.openstreetmap.org/node/{node.Id}";
                    StopPositions.Add((nodeId, coordinate, el.Tags));
                }
            }

            if (ts.ContainsKey("opening_hours"))
            {
                OpeningTimes = OpeningHours.Parse(ts["opening_hours"], GetTimeZone()) ?? new TwentyFourSeven();
            }
            else
            {
                OpeningTimes = new TwentyFourSeven();
            }
        }


        /// <summary>
        /// Gets the timezone of this route.
        /// This is based on the lat/lon of the first stop, which is in turn passed into the GeoTimeZone-package.
        /// This packages uses the OSM-boundaries to determine country and thus timezone
        /// </summary>
        public string GetTimeZone()
        {
            var coordinate = StopPositions[0].Item2;
            return TimeZoneLookup.GetTimeZone(coordinate.X, coordinate.Y).Result;
        }


        /// <summary>
        ///  Tries to figure out where the relation is located.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<OsmRoute> LoadFrom(string path)
        {
            // We strip of everything until we only keep the number identifying the relation
            // That, we pass onto the API of OSM

            if (path.StartsWith("https://www.openstreetmap.org/relation/"))
            {
                path = path.Substring("https://www.openstreetmap.org/relation/".Length)
                    .Split('?')[0];
            }

            if (path.StartsWith("http://www.openstreetmap.org/relation/"))
            {
                path = path.Substring("http://www.openstreetmap.org/relation/".Length)
                    .Split('?')[0];
            }

            if (long.TryParse(path, out _))
            {
                path =
                    $"https://openstreetmap.org/api/0.6/relation/{path}/full";
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (path.StartsWith("http"))
            {
                return LoadFromUrl(new Uri(path));
            }
            else
            {
                // We are probably dealing with a path
                using (var stream = File.OpenRead(path))
                {
                    return OsmRouteFromStream(stream);
                }
            }
        }

        private static List<OsmRoute> LoadFromUrl(Uri path)
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