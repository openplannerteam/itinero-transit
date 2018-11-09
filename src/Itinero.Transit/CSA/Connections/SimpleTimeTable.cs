using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit
{
    public class SimpleTimeTable : ITimeTable
    {
        private readonly IEnumerable<IConnection> _cons;
        private readonly DateTime start, end;

        public SimpleTimeTable(IEnumerable<IConnection> cons)
        {
            _cons = cons;

            foreach (var con in cons)
            {
                if (start == null || con.DepartureTime() < start)
                {
                    start = con.DepartureTime();
                }

                if (end == null || con.ArrivalTime() > end)
                {
                    end = con.ArrivalTime();
                }
            }
        }
        
        public DateTime StartTime()
        {
            return start;
        }

        public DateTime EndTime()
        {
            return end;
        }

        public DateTime PreviousTableTime()
        {
            return start;
        }

        public DateTime NextTableTime()
        {
            return end;
        }

        public Uri NextTable()
        {
            return new Uri("http://example.com/tt/2");
        }

        public Uri PreviousTable()
        {
            return new Uri("http://example.com/tt/0");
        }

        public Uri Id()
        {
           return new Uri("http://example.com/tt/1");
        }

        public IEnumerable<IConnection> Connections()
        {
            return _cons;
        }

        public IEnumerable<IConnection> ConnectionsReversed()
        {
            return _cons.Reverse();
        }

        public string ToString(ILocationProvider locationDecoder)
        {
            return "TestTT";
        }

        public string ToString(ILocationProvider locationDecoder, List<Uri> stopsWhitelist)
        {
            return "TestTT";
        }
    }
}