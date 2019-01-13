using System;
using System.IO;
using Reminiscence.Arrays;

namespace Itinero.Transit.Data
{
    internal static class MemoryArrayExtensions
    {
        /// <summary>
        /// Creates a new memory array reading the size and copying from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>A memory array.</returns>
        public static MemoryArray<T> CopyFromWithSize<T>(this Stream stream)
        {
            var buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            var length = BitConverter.ToInt64(buffer, 0);
            
            var memoryArray = new MemoryArray<T>(length);
            memoryArray.CopyFrom(stream);
            return memoryArray;
        }
    }
}