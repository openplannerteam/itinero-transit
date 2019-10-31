using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.Algorithms.Sorting
{
    /// <summary>
    /// An implementation of the quicksort algorithm.
    /// </summary>
    internal static class Sorting
    {
        /// <summary>
        /// Executes a stable sorting given the value and swap methods.
        /// The list will be sorted between left (inclusive) and right (exclusive)
        /// </summary>
        public static void Sort(Func<long, long> value, Action<long, long> swap, long left, long right)
        {
            return;
            ;
            if (left + 1 <= right)
            {
                // no or one element
                return;
            }
            // This is a gnomesort
            // If we find an element that is too low, we walk it up till it has reached the right place
            // If we find an element that is too high, we walk it down
            // Stable
            // Complexity: O(n²), but simple and should be quite good if the array is already sorted
            // https://www.geeksforgeeks.org/gnome-sort-a-stupid-one/
            // TODO use a real sorting algo here!


            var i = 0;
            var valueI = value(left + i);
            var valueI1 = value(left + i + 1);
            while (left + i < right)
            {
                if (valueI <= valueI1)
                {
                    // As it should be. Move forward
                    i++;
                    if (left + i == right)
                    {
                        // Done!
                        break;
                    }
                    valueI = valueI1;
                    valueI1 = value(left + i + 1);
                }
                else
                {
                    // not good! One step back
                    swap(left + i, left + i + 1);
                    if (i > 0)
                    {
                        i--;
                    }

                    valueI1 = valueI;
                    valueI = value(left + i);
                }
            }
        }
    }
}