using System.Collections;
using System.Collections.Generic;

namespace Itinero.Transit.Data.Aggregators
{
    /// <summary>
    /// Equivalent to monad bind
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumeratorAggregator<T> : IEnumerator<T>

    {
        private readonly IEnumerator<IEnumerator<T>> _enumerators;

        public EnumeratorAggregator(IEnumerator<IEnumerator<T>> enumerators)
        {
            _enumerators = enumerators;
            _enumerators.MoveNext();
        }

        public bool MoveNext()
        {
            do
            {
                if (_enumerators.Current?.MoveNext() ?? false)
                {
                    return true;
                }
            } while (_enumerators.MoveNext());

            return false;
        }

        public void Reset()
        {
            _enumerators.Reset();
            while(_enumerators.MoveNext())
            {
                _enumerators.Current?.Reset();
            }
            _enumerators.Reset();
            _enumerators.MoveNext();
        }

        public void Dispose()
        {
            _enumerators.Reset();
             while(_enumerators.MoveNext())
            {
                _enumerators.Current?.Dispose();
            }
        }

        public T Current => _enumerators.Current.Current;

        object IEnumerator.Current => Current;
    }
}