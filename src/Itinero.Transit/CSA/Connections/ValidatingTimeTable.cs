using System;
using System.Collections;
using System.Collections.Generic;

namespace Itinero.Transit
{
    /// <summary>
    /// The validating time table is a wrapper around a normal timetable.
    /// It filters out faulty data that should be fixed upstream.
    /// </summary>
    public class ValidatingTimeTable : ITimeTable
    {
        private readonly ITimeTable _backdrop;

        public ValidatingTimeTable(ITimeTable backdrop)
        {
            _backdrop = backdrop;
        }


        public IEnumerable<IConnection> Connections()
        {
            return new ValidatingEnumerable(_backdrop.Connections());
        }

        public IEnumerable<IConnection> ConnectionsReversed()
        {
            return new ValidatingEnumerable(_backdrop.ConnectionsReversed());
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

        public ValidatingEnumerable(IEnumerable<IConnection> backdrop)
        {
            _backdrop = backdrop;
        }

        public IEnumerator<IConnection> GetEnumerator()
        {
            return new ValidatingEnumerator(_backdrop.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    class ValidatingEnumerator : IEnumerator<IConnection>
    {
        private readonly IEnumerator<IConnection> _backdrop;
        private readonly HashSet<IConnection> _alreadySeen = new HashSet<IConnection>();

        public ValidatingEnumerator(IEnumerator<IConnection> backdrop)
        {
            _backdrop = backdrop;
        }

        public bool MoveNext()
        {
            bool found = _backdrop.MoveNext();
            while (found && _alreadySeen.Contains(_backdrop.Current))
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

        public IConnection Current { get; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _backdrop.Dispose();
        }
    }
}