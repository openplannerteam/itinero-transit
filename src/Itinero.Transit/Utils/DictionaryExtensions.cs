using System.Collections.Generic;

namespace Itinero.Transit.Utils
{
    public static class DictionaryExtensions
    {

        public static void AddTo<K, T>(this Dictionary<K, HashSet<T>> d, K key, T value)
        {
            if (d.TryGetValue(key, out var list))
            {
                list.Add(value);
            }
            else
            {
                d[key] = new HashSet<T>
                {
                    value
                };
            }
        }
        
    }
}