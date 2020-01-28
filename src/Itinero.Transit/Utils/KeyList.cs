using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Utils
{
    public class KeyList<T> : IEnumerable<T>, IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _keys;

        public KeyList(IEnumerable<T> keys)
        {
            _keys = keys.ToList();
        }

        private bool Equals(KeyList<T> other)
        {
            return _keys.SequenceEqual(other._keys);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _keys.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KeyList<T>) obj);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var k in _keys)
            {
                hash += k.GetHashCode();
            }

            return hash;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _keys.Count;

        public T this[int index] => _keys[index];
    }
}