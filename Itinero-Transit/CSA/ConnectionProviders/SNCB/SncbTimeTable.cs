using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Represents one entire page of connections
    /// </summary>
    [Serializable]
    public class SncbTimeTable : LinkedObject, ITimeTable
    {
        public Uri Next { get; set; }
        public Uri Prev { get; set; }

        private DateTime _startTime, _endTime;

        public List<IConnection> Graph { get; set; }

        public SncbTimeTable(Uri uri) : base(uri)
        {
        }


        public SncbTimeTable(JObject json) : base(new Uri(json["@id"].ToString()))
        {
            FromJson(json);
        }

        protected sealed override void FromJson(JObject json)
        {
            File.WriteAllText("debug.json", json.ToString());
            Uri = AsUri(json["@id"].ToString());
            Next = AsUri(json["hydra:next"].ToString());
            Prev = AsUri(json["hydra:previous"].ToString());
            _startTime = _extractTime(AsUri(json["@id"].ToString()));
            _endTime = _extractTime(Next);


            Graph = new List<IConnection>();
            var jsonGraph = json["@graph"];
            foreach (var conn in jsonGraph)
            {
                try
                {
                    Graph.Add(new SncbConnection((JObject) conn));
                }
                catch (ArgumentException e)
                {
                    Log.Warning(e, "Ignored connection due to time travelling");
                }
            }
        }

        private DateTime _extractTime(Uri u)
        {
            var raw = u.OriginalString;
            var ind = raw.IndexOf("departureTime=", StringComparison.Ordinal);
            if (ind < 0)
            {
                throw new ArgumentException("The passed URI does not contain a departureTime argument");
            }

            var start = ind + "departureTime=".Length;
            var time = raw.Substring(start, raw.Length-start-2);
            return DateTime.Parse(time);
        }

        public override string ToString()
        {
            var res =
                $"Timetable with {Graph.Count} connections; ID: {Uri.Segments.Last()} Next: {Next.Segments.Last()} Prev: {Prev.Segments.Last()}\n";
            foreach (var conn in Graph)
            {
                res += $"  {conn}\n";
            }

            res = res.Substring(0, res.Length - 1);
            return res;
        }

        public DateTime StartTime()
        {
            return _startTime;
        }

        public DateTime EndTime()
        {
            return _endTime;
        }

        public Uri NextTable()
        {
            return Next;
        }

        public Uri PreviousTable()
        {
            return Prev;
        }

        public List<IConnection> Connections()
        {
            return Graph;
        }

    }
}