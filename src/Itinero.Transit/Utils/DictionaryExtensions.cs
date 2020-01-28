using System.Collections.Generic;

namespace Itinero.Transit.Utils
{
    public static class DictionaryExtensions
    {

        // ReSharper disable once InconsistentNaming
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