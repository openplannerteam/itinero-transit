using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Represents one entire page of connections
    /// </summary>
    public class TimeTable : LinkedObject
    {

        public Uri next { get; set; }
        public Uri prev { get; set; }

        public List<Connection> graph = new List<Connection>();
        
        
        public TimeTable(Uri uri) : base(uri)
        {
        }
        
        
        public TimeTable(JToken json) : base(new Uri(json["@id"].ToString()))
        {
            next = new Uri(json["hydra:next"].ToString());
            prev = new Uri(json["hydra:previous"].ToString());

            var jsonGraph = json["@graph"];
            var l = jsonGraph.Count();
            foreach (var conn in jsonGraph)
            {
                graph.Add(new Connection(conn));
            }
        }

        public override string ToString()
        {
            var res = $"Timetable with {graph.Count} conneections; ID: {Uri.Segments.Last()} Next: {next.Segments.Last()} Prev: {prev.Segments.Last()}\n";
            foreach (var conn in graph)
            {
                res += $"  {conn}\n";
            }

            res = res.Substring(0, res.Length - 1);
            return res;
        }
    }
}