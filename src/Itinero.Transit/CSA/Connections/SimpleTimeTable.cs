using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit
{
    public class SimpleTimeTable : ITimeTable
    {
        private IEnumerable<IConnection> _cons;
        private DateTime _start, _end;

        public SimpleTimeTable(IEnumerable<IConnection> cons)
        {
            _cons = cons;

            foreach (var con in _cons)
            {
                if (_start == null || con.DepartureTime() < _start)
                {
                    _start = con.DepartureTime();
                }

                if (_end == null || con.ArrivalTime() > _end)
                {
                    _end = con.ArrivalTime();
                }
            }
        }
        
        public DateTime StartTime()
        {
            return _start;
        }

        public DateTime EndTime()
        {
            return _end;
        }

        public DateTime PreviousTableTime()
        {
            return _start;
        }

        public DateTime NextTableTime()
        {
            return _end;
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