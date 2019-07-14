using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class ConnectionEnumeratorAggregator : IConnectionEnumerator
    {
        private readonly IConnectionEnumerator[] _enumerators;
        private readonly bool[] _canContinue;
        private uint _currentBest;


        private ConnectionEnumeratorAggregator(IEnumerable<IConnectionEnumerator> sources)
        {
            _enumerators = sources.ToArray();
            _canContinue = new bool[_enumerators.Length];
        }


        public void MoveTo(ulong dateTime)
        {
            foreach (var e in _enumerators)
            {
                e.MoveTo(dateTime);
            }

            _currentBest = uint.MaxValue;
        }


        public bool HasNext()
        {
            // Actual initialization
            if (_currentBest == uint.MaxValue)
            {
                var oneFound = false;
                var lowestTime = ulong.MaxValue;
                for (var i = 0; i < _enumerators.Length; i++)
                {
                    var ei = _enumerators[i];
                    var found = ei.HasNext();
                    oneFound |= found;
                    _canContinue[i] = found;
                    // ReSharper disable once InvertIf
                    if (found && ei.CurrentDateTime < lowestTime)
                    {
                        _currentBest = (uint) i;
                        lowestTime = ei.CurrentDateTime;
                    }
                }

                return oneFound;
            }


            // We increase the enumerator which has been read: namely the current lowest
            // We keep track of its date though...
            var curLow = _enumerators[_currentBest];
            var oldDate = curLow.CurrentDateTime;
            if (!curLow.HasNext())
            {
                _canContinue[_currentBest] = false;
            }
            else if (oldDate == curLow.CurrentDateTime)
            {
                // Current lowest does not change and has a next value
                // All done
                return true;
            }

            var nextFound = false;
            _currentBest = int.MaxValue;
            var currentLowestTime = ulong.MaxValue;
            // We need to search a new currentLowest
            for (var i = 0; i < _enumerators.Length; i++)
            {
                if (!_canContinue[i])
                {
                    continue;
                }

                if (_enumerators[i].CurrentDateTime < currentLowestTime)
                {
                    _currentBest = (uint) i;
                    currentLowestTime = _enumerators[i].CurrentDateTime;
                    nextFound = true;
                }
            }

            // All the non-depleted things should be ready to give a new value
            return nextFound;
        }


        public bool HasPrevious()
        {
            // Actual initialization
            if (_currentBest == uint.MaxValue)
            {
                var oneFound = false;
                var lowestTime = ulong.MinValue;
                for (var i = 0; i < _enumerators.Length; i++)
                {
                    var ei = _enumerators[i];
                    var found = ei.HasPrevious();
                    oneFound |= found;
                    _canContinue[i] = found;
                    // ReSharper disable once InvertIf
                    if (found && ei.CurrentDateTime > lowestTime)
                    {
                        _currentBest = (uint) i;
                        lowestTime = ei.CurrentDateTime;
                    }
                }

                return oneFound;
            }


            // We increase the enumerator which has been read: namely the current best
            // We keep track of its date though...
            var curHigh = _enumerators[_currentBest];
            var oldDate = curHigh.CurrentDateTime;
            if (!curHigh.HasPrevious())
            {
                // No new value from the current iterator
                // We mark this one depleted and continue to the new 'election'
                _canContinue[_currentBest] = false;
            }
            else if (oldDate == curHigh.CurrentDateTime)
            {
                // Current best does not change and has a next value
                // All done
                return true;
            }

            // The election of the new best iterator
            var nextFound = false;
            _currentBest = int.MaxValue;
            var currentHighestTime = ulong.MinValue;
            // We need to search a new currentLowest
            for (var i = 0; i < _enumerators.Length; i++)
            {
                if (!_canContinue[i])
                {
                    continue;
                }

                if (_currentBest == int.MaxValue ||
                    _enumerators[i].CurrentDateTime > currentHighestTime)
                {
                    _currentBest = (uint) i;
                    currentHighestTime = _enumerators[i].CurrentDateTime;
                    nextFound = true;
                }
            }

            return nextFound;
        }

        public bool Current(Connection toWrite)
        {
            return _enumerators[_currentBest].Current(toWrite);
        }

        public ulong CurrentDateTime => _enumerators[_currentBest].CurrentDateTime;

        public static IConnectionEnumerator CreateFrom(IConnectionEnumerator a, IConnectionEnumerator b)
        {
            return CreateFrom(new[] {a, b});
        }

        public static IConnectionEnumerator CreateFrom(IEnumerable<IConnectionEnumerator> sources)
        {
            if (sources.Count() == 1)
            {
                return sources.First();
            }

            return new ConnectionEnumeratorAggregator(sources);
        }
    }
}