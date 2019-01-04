using System;
using System.Collections.Generic;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.IO.LC.CSA.Connections
{
    /// <summary>
    /// Represents one entire page of connections, based on a LinkedConnections JSON-LD
    /// </summary>
    [Serializable]
    internal class LinkedTimeTable : LinkedObject, ITimeTable
    {
        private Uri Next { get; set; }
        private Uri Prev { get; set; }

        private DateTime _startTime, _endTime;

        private List<LinkedConnection> Graph { get; set; }
        [NonSerializedAttribute] private List<LinkedConnection> _reversedGraph;

        public LinkedTimeTable(Uri uri) : base(uri)
        {
        }


        public LinkedTimeTable(JObject json) : base(new Uri(json["@id"].ToString()))
        {
            FromJson(json);
        }

        protected sealed override void FromJson(JObject json)
        {
            if(!json.IsType("http://www.w3.org/ns/hydra/core#PartialCollectionView") && 
               !json.IsType("http://www.w3.org/ns/hydra/core#PagedCollection"))
            {
                throw new ArgumentException("The passed JSON does not follow the expected ontology");                
            }

            Next = new Uri(json["http://www.w3.org/ns/hydra/core#next"][0]["@id"].ToString());
            Prev = new Uri(json["http://www.w3.org/ns/hydra/core#previous"][0]["@id"].ToString());
            _startTime = _extractTime(new Uri(json["@id"].ToString()));
            _endTime = _extractTime(Next);


            Graph = new List<LinkedConnection>();
            var jsonGraph = json["@graph"];
            foreach (var conn in jsonGraph)
            {
                try
                {
                    Graph.Add(new LinkedConnection((JObject) conn));
                }
                catch (ArgumentException e)
                {
                    Log.Warning(e, "Connection ignored due to exception");
                }
            }
        }

        private static DateTime _extractTime(Uri u)
        {
            var raw = u.OriginalString;
            var ind = raw.IndexOf("departureTime=", StringComparison.Ordinal);
            if (ind < 0)
            {
                throw new ArgumentException("The passed URI does not contain a departureTime argument");
            }

            var start = ind + "departureTime=".Length;
            var time = raw.Substring(start, raw.Length - start - 2);
            return DateTime.Parse(time);
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(ILocationProvider locationDecoder)
        {
            return ToString(locationDecoder, null);
        }

        public string ToString(ILocationProvider locationDecoder, List<Uri> whitelist)
        {
            var omitted = 0;
            var cons = "  ";
            foreach (var conn in Graph)
            {
                if (whitelist != null && !whitelist.Contains(conn.DepartureLocation())
                                      && !whitelist.Contains(conn.ArrivalLocation()))
                {
                    omitted++;
                    continue;
                }

                cons += $"  {conn}\n";
            }

            cons = cons.Substring(0, cons.Length - 1);

            var header =
                $"Timetable with {Graph.Count} connections ({omitted} omitted below);" +
                $" ID: {Uri} Next: {Next} Prev: {Prev}\n";
            return header + cons;
        }

        public DateTime StartTime()
        {
            return _startTime;
        }

        public DateTime EndTime()
        {
            return _endTime;
        }

        public DateTime PreviousTableTime()
        {
            return _extractTime(Prev);
        }

        public DateTime NextTableTime()
        {
          return  _extractTime(Next);
        }

        public Uri NextTable()
        {
            return Next;
        }

        public Uri PreviousTable()
        {
            return Prev;
        }

        public IEnumerable<LinkedConnection> Connections()
        {
            return Graph;
        }

        public IEnumerable<LinkedConnection> ConnectionsReversed()
        {
            if (_reversedGraph == null)
            {
                _reversedGraph = new List<LinkedConnection>(Graph);
                _reversedGraph.Reverse();
            }

            return _reversedGraph;
        }
    }
}