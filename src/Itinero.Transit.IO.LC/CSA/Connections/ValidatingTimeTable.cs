using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;
namespace Itinero.IO.LC
{
    /// <summary>
    /// The validating time table is a wrapper around a normal timetable.
    /// It filters out faulty data that should be fixed upstream.
    /// </summary>
    public class ValidatingTimeTable : ITimeTable
    {
        private readonly ILocationProvider _locations;
        private readonly ITimeTable _backdrop;

        public ValidatingTimeTable(ILocationProvider locations, ITimeTable backdrop)
        {
            _locations = locations;
            _backdrop = backdrop;
        }


        public IEnumerable<IConnection> Connections()
        {
            return new ValidatingEnumerable(_backdrop.Connections(), _backdrop, _locations);
        }

        public IEnumerable<IConnection> ConnectionsReversed()
        {
            return new ValidatingEnumerable(_backdrop.ConnectionsReversed(), _backdrop, _locations);
        }


        public DateTime StartTime()
        {
            return _backdrop.StartTime();
        }

        public DateTime EndTime()
        {
            return _backdrop.EndTime();
        }

        public DateTime PreviousTableTime()
        {
            return _backdrop.PreviousTableTime();
        }

        public DateTime NextTableTime()
        {
            return _backdrop.NextTableTime();
        }

        public Uri NextTable()
        {
            return _backdrop.NextTable();
        }

        public Uri PreviousTable()
        {
            return _backdrop.PreviousTable();
        }

        public Uri Id()
        {
            return _backdrop.Id();
        }

        public string ToString(ILocationProvider locationDecoder)
        {
            return _backdrop.ToString(locationDecoder);
        }

        public string ToString(ILocationProvider locationDecoder, List<Uri> stopsWhitelist)
        {
            return _backdrop.ToString(locationDecoder, stopsWhitelist);
        }
    }

   public  class ValidatingEnumerable : IEnumerable<IConnection>
    {
        private readonly IEnumerable<IConnection> _backdrop;
        private readonly ITimeTable _source;
        private readonly ILocationProvider _locations;

        public ValidatingEnumerable(IEnumerable<IConnection> backdrop, ITimeTable source, ILocationProvider locations)
        {
            _backdrop = backdrop;
            _source = source;
            _locations = locations;
        }

        public IEnumerator<IConnection> GetEnumerator()
        {
            return new ValidatingEnumerator(_source, _backdrop.GetEnumerator(), _locations);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    class ValidatingEnumerator : IEnumerator<IConnection>
    {
        private readonly ITimeTable _source;
        private readonly IEnumerator<IConnection> _backdrop;
        private readonly HashSet<IConnection> _alreadySeen = new HashSet<IConnection>();
        private readonly ILocationProvider _location;

        public ValidatingEnumerator(ITimeTable source, IEnumerator<IConnection> backdrop, ILocationProvider location)
        {
            _source = source;
            _backdrop = backdrop;
            _location = location;
        }


        private bool IsValid(IConnection current)
        {

            if (_alreadySeen.Contains(current))
            {
#if DEBUG
                Log.Warning($"Already seen this connection.\nDuplicate connection:{current}\nTable:{_source.Id()}");
#endif
                return false;
            }

            if (!_location.ContainsLocation(current.DepartureLocation()) ||
                !_location.ContainsLocation(current.ArrivalLocation()))
            {
                Log.Warning("Connection contains unknown stations. The locations fragment might be out of date.");
                return false;
            }

            return true;

        }
        
        public bool MoveNext()
        {
            bool found = _backdrop.MoveNext();
            while (found && !IsValid(_backdrop.Current))
            {
                found = _backdrop.MoveNext();
            }

            if (found)
            {
                _alreadySeen.Add(_backdrop.Current);
                Current = _backdrop.Current;
            }

            return found;
        }

        public void Reset()
        {
            _alreadySeen.Clear();
            _backdrop.Reset();
        }

        public IConnection Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _backdrop.Dispose();
        }
    }
}