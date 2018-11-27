using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit
{
    /// <inheritdoc />
    /// <summary>
    /// A synthetic Time Table merges multiple time tables into one.
    /// It is used to offer intermodality
    /// </summary>
    public class SyntheticTimeTable : ITimeTable
    {
        private readonly DateTime _startTime, _endTime, _previousTime;
        private readonly Uri _uri;

        private List<ITimeTable> _sources;

        public SyntheticTimeTable(IReadOnlyCollection<ITimeTable> sources, Uri uri)
        {
            // We make a copy, the parent list might change
            _sources = new List<ITimeTable>(sources);
            _uri = uri;
            DateTime? startTime = null;
            DateTime? endTime = null;
            DateTime? previousTime = null;
            foreach (var tt in sources)
            {
                if (startTime == null)
                {
                    startTime = tt.StartTime();
                    previousTime = startTime;
                }

                if (endTime == null)
                {
                    endTime = tt.EndTime();
                }

                if (tt.StartTime() > startTime)
                {
                    previousTime = startTime;
                    startTime = tt.StartTime();
                }

                if (tt.EndTime() < endTime)
                {
                    endTime = tt.EndTime();
                }
            }

            if (startTime == null || endTime == null || endTime < startTime)
            {
                throw new ArgumentException(
                    "Synthetic TimeTable: make sure that you pass at least one timetable and that all timetables have some overlapping time");
            }

            _startTime = (DateTime) startTime;
            _endTime = (DateTime) endTime;

            // The previous time is the second hightest start time that we encountered
            // There is a special case though, namely if all starting times of all tables coincide
            // Then, we query each timetable for the predecessor and pick out the earliest
            if (previousTime.Equals(startTime))
            {
                previousTime = null;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var tt in sources)
                {
                    if (previousTime == null)
                    {
                        previousTime = tt.PreviousTableTime();
                        continue;
                    }

                    if (tt.PreviousTableTime() > previousTime)
                    {
                        previousTime = tt.PreviousTableTime();
                    }
                }
            }

            if (previousTime == null)
            {
                throw new ArgumentException("Could not determine a previous table");
            }

            _previousTime = (DateTime) previousTime;
        }


        public Uri NextTable()
        {
            return new Uri($"{ConnectionProviderMerger.SyntheticUri}{NextTableTime():O}");
        }

        public Uri PreviousTable()
        {
            return new Uri($"{ConnectionProviderMerger.SyntheticUri}{_previousTime:O}");
        }


        public IEnumerable<IConnection> Connections()
        {
            return new EnumeratorMerger(_sources, _startTime, _endTime);
        }

        public IEnumerable<IConnection> ConnectionsReversed()
        {
            var cons = new List<IConnection>(Connections());
            cons.Reverse();
            return cons;
        }


        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(ILocationProvider locationDecoder)
        {
            return ToString(locationDecoder, null);
        }

        public string ToString(ILocationProvider locationDecoder, List<Uri> stopsWhitelist)
        {
            return $"Synthetic timetable {_uri}";
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
            return _previousTime;
        }

        public DateTime NextTableTime()
        {
            return _endTime;
        }

        public Uri Id()
        {
            return _uri;
        }
    }

    /// <summary>
    /// This enumerator takes multiple sources and takes the earliest departure of each
    /// </summary>
    public class EnumeratorMerger : IEnumerator<IConnection>, IEnumerable<IConnection>
    {
        public IConnection Current { get; private set; }

        private readonly List<IEnumerator<IConnection>> _sources;

        private readonly DateTime _startTime, _endTime;

        public EnumeratorMerger(IEnumerable<ITimeTable> sources, DateTime startTime, DateTime endTime)
        {
            var sourcesList = new List<IEnumerator<IConnection>>();
            _startTime = startTime;
            _endTime = endTime;

            foreach (var tt in sources)
            {
                var iEnum = tt.Connections();
                var enumerator = iEnum.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    // We set the enumerators at their valid first entries
                    // Note that empty timetables are skipped
                    if (enumerator.Current.DepartureTime() >= startTime &&
                        enumerator.Current.DepartureTime() < endTime)
                    {
                        sourcesList.Add(enumerator);
                        break;
                    }
                }
            }

            _sources = sourcesList;
        }


        public bool MoveNext()
        {
            IEnumerator<IConnection> actualSource = null;
            Current = null;
            for (var i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                var cur = source.Current;
                if (_startTime > cur.DepartureTime() || cur.DepartureTime() >= _endTime)
                {
                    // Source is depleted: out of range
                    _sources.Remove(source);
                    i--;
                    continue;
                }

                // we have found a valid entry
                if (Current == null ||
                    Current.DepartureTime() > cur.DepartureTime()){
                    Current = cur;
                    actualSource = source;
                }
            }

            if (actualSource != null && !actualSource.MoveNext())
            {
                _sources.Remove(actualSource);
            }

            return Current != null;
        }

        public void Reset()
        {
            Current = null;
            foreach (var source in _sources)
            {
                source.Reset();
            }
        }


        object IEnumerator.Current => Current;

        public void Dispose()
        {
            foreach (var source in _sources)
            {
                source.Dispose();
            }
        }

        public IEnumerator<IConnection> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}