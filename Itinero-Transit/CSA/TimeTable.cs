using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Represents one entire page of connections
    /// </summary>
    public class TimeTable : LinkedObject
    {

        public Uri Next { get; set; }
        public Uri Prev { get; set; }

        public List<Connection> Graph { get; set; }
        
        public TimeTable(Uri uri) : base(uri)
        {
        }
        
        
        public TimeTable(JToken json) : base(new Uri(json["@id"].ToString()))
        {
           FromJson(json);
        }

        protected sealed override void FromJson(JToken json)
        {
            Next = AsUri(json["hydra:next"].ToString());
            Prev = AsUri(json["hydra:previous"].ToString());

            Graph = new List<Connection>();
            var jsonGraph = json["@graph"];
            foreach (var conn in jsonGraph)
            {
                Graph.Add(new Connection(conn));
            }
        }

        public override string ToString()
        {
            var res = $"Timetable with {Graph.Count} conneections; ID: {Uri.Segments.Last()} Next: {Next.Segments.Last()} Prev: {Prev.Segments.Last()}\n";
            foreach (var conn in Graph)
            {
                res += $"  {conn}\n";
            }

            res = res.Substring(0, res.Length - 1);
            return res;
        }
    }
}