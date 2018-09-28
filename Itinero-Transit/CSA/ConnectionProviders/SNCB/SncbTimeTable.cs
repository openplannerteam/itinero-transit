using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Represents one entire page of connections
    /// </summary>
    public class TimeTable : LinkedObject, ITimeTable
    {

        public Uri Next { get; set; }
        public Uri Prev { get; set; }

        private DateTime _startTime, _endTime;

        public List<PTConnection> Graph { get; set; }
        
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

            _startTime = _extractTime(AsUri(json["@id"].ToString()));
            _endTime = _extractTime(Next);
            

            Graph = new List<PTConnection>();
            var jsonGraph = json["@graph"];
            foreach (var conn in jsonGraph)
            {
                try
                {
                    Graph.Add(new PTConnection(conn));
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

            var time = raw.Substring(ind + "departureTime=".Length, raw.Length);
            return DateTime.Parse(time, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            var res = $"Timetable with {Graph.Count} connections; ID: {Uri.Segments.Last()} Next: {Next.Segments.Last()} Prev: {Prev.Segments.Last()}\n";
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

        public IEnumerable<IConnection> Connections()
        {
            return Graph;
        }

    }
}