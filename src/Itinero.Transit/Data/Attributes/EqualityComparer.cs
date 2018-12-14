using System.Collections.Generic;

namespace Itinero.Transit.Data.Attributes
{
    /// <summary>
    /// An implementation of the EqualityComparer that allows the use of delegates.
    /// </summary>
    internal sealed class EqualityComparer : IEqualityComparer<int[]>
    {
        /// <summary>
        /// Returns true if the two given objects are considered equal.
        /// </summary>
        public bool Equals(int[] x, int[] y)
        {
            if (x == null && y == null)
            {
                return false;
            }

            if (x == null || y == null)
            {
                return false;
            }
            
            if (x.Length != y.Length) return false;
            for (var idx = 0; idx < x.Length; idx++)
            {
                if (x[idx] != y[idx])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculates the hashcode for the given object.
        /// </summary>
        public int GetHashCode(int[] obj)
        {
            var hash = obj.Length.GetHashCode();
            foreach (var t in obj)
            {
                hash = hash ^ t.GetHashCode();
            }
            return hash;
        }
    }
}