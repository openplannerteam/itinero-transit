using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Itinero.Attributes;
using Itinero.Transit.Algorithms.Sorting;
using Reminiscence.Arrays;
using Reminiscence.IO;
using Reminiscence.IO.Streams;

namespace Itinero.Transit.Data.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// A collection that contains meta-data per unique id, can be used to map meta-data to vertices or edges by their id's.
    /// </summary>
    public class MappedAttributesIndex : IEnumerable<uint>
    {
        private const int _blockSize = 1024;
        private const uint _noData = uint.MaxValue;
        private readonly ArrayBase<uint> _data; // holds pairs of id's and a pointer to the attribute collection for that id.
        private readonly AttributesIndex _attributes;

        /// <summary>
        /// Creates a new mapped attributes index.
        /// </summary>
        public MappedAttributesIndex(AttributesIndexMode mode = AttributesIndexMode.ReverseCollectionIndex |
                AttributesIndexMode.ReverseStringIndex)
        {
            _data = new MemoryArray<uint>(1024);
            _attributes = new AttributesIndex(mode);
            _reverseIndex = new Dictionary<uint, int>();

            for (var p = 0; p < _data.Length; p++)
            {
                _data[p] = _noData;
            }
        }

        /// <summary>
        /// Creates a new mapped attributes index.
        /// </summary>
        public MappedAttributesIndex(MemoryMap map,
            AttributesIndexMode mode = AttributesIndexMode.ReverseCollectionIndex |
                AttributesIndexMode.ReverseStringIndex)
        {
            _data = new Array<uint>(map, 1024);
            _attributes = new AttributesIndex(map, mode);
            _reverseIndex = new Dictionary<uint, int>();

            for (var p = 0; p < _data.Length; p++)
            {
                _data[p] = _noData;
            }
        }
        
        /// <summary>
        /// Used for deserialization.
        /// </summary>
        private MappedAttributesIndex(ArrayBase<uint> data, AttributesIndex attributes)
        {
            _data = data;
            _pointer = (int)_data.Length;
            _attributes = attributes;

            _reverseIndex = null;
    }

        private Dictionary<uint, int> _reverseIndex;
        private int _pointer;

        /// <summary>
        /// Gets or sets attributes for the given id.
        /// </summary>
        public IAttributeCollection this[uint id]
        {
            get
            {
                var p = Search(id, out _);
                if (p == _noData)
                {
                    return null;
                }
                return _attributes.Get(p);
            }
            set
            {
                var p = Search(id, out var idx);
                if (p == _noData)
                {
                    Add(id, _attributes.Add(value));
                    return;
                }
                _data[idx + 1] = _attributes.Add(value);
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<uint> GetEnumerator()
        {
            for (var p = 0; p < _pointer; p += 2)
            {
                yield return _data[p];
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns true if this index is sorted and optimized.
        /// </summary>
        public bool IsOptimized => _reverseIndex == null;

        /// <summary>
        /// Optimizes this index once it's fully loaded.
        /// </summary>
        public void Optimize()
        {
            // sort array.
            QuickSort.Sort(i => _data[i * 2], (i, j) =>
            {
                var t1 = _data[i * 2 + 0];
                var t2 = _data[i * 2 + 1];
                _data[i * 2 + 0] = _data[j * 2 + 0];
                _data[i * 2 + 1] = _data[j * 2 + 1];
                _data[j * 2 + 0] = t1;
                _data[j * 2 + 1] = t2;
            },
            0, (_pointer / 2) - 1);

            // remove reverse index.
            _reverseIndex = null;

            // reduce array size to exact data size.
            _data.Resize(_pointer);
        }

        /// <summary>
        /// Makes this index writable again, once made writeable it will use more memory and be less efficient, use optimize again once the data is updated.
        /// </summary>
        public void MakeWriteable()
        {
            _reverseIndex = new Dictionary<uint, int>();
            for (var p = 0; p < _data.Length; p += 2)
            {
                if (_data[p + 0] == _noData)
                {
                    continue;
                }
                _reverseIndex[_data[p + 0]] = p + 0;
            }
        }

        /// <summary>
        /// Serializes to the given stream, after optimizing the index, returns the # of bytes written.
        /// </summary>
        public long Serialize(Stream stream)
        {
            if (!IsOptimized)
            {
                Optimize();
            }

            long size = 1;
            // write the version #
            // 1: initial version.
            stream.WriteByte(1);

            // write data size.
            var bytes = BitConverter.GetBytes((uint)_data.Length);
            stream.Write(bytes, 0, 4);
            size += 4;

            // write data.
            size += _data.CopyTo(stream);

            // write attributes.
            size += _attributes.Serialize(stream);

            return size;
        }

        /// <summary>
        /// Switches the two id's.
        /// </summary>
        public void Switch(uint id1, uint id2)
        {
            if (_reverseIndex == null)
            {
                MakeWriteable();
            }

            // remove the two from the index and keep their pointers.
            if (!_reverseIndex.TryGetValue(id1, out var pointer1))
            {
                pointer1 = -1;
            }
            else
            {
                _reverseIndex.Remove(id1);
            }
            if (!_reverseIndex.TryGetValue(id2, out var pointer2))
            {
                pointer2 = -1;
            }
            else
            {
                _reverseIndex.Remove(id2);
            }

            // add them again but in reverse.
            if (pointer1 != -1)
            {
                _data[pointer1] = id2;
                _reverseIndex[id2] = pointer1;
            }
            if (pointer2 != -1)
            {
                _data[pointer2] = id1;
                _reverseIndex[id1] = pointer2;
            }
        }

        /// <summary>
        /// Deserializes from the given stream, returns an optimized index.
        /// </summary>
        public static MappedAttributesIndex Deserialize(Stream stream, MappedAttributesIndexProfile profile)
        {
            var version = stream.ReadByte();
            if (version > 1)
            {
                throw new Exception(
                    $"Cannot deserialize mapped attributes index: Invalid version #: {version}, upgrade Itinero.");
            }

            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            var length = BitConverter.ToUInt32(bytes, 0);

            ArrayBase<uint> data;
            if (profile == null || profile.DataProfile == null)
            {
                data = new MemoryArray<uint>(length);
                data.CopyFrom(stream);
            }
            else
            {
                var position = stream.Position;
                var map = new MemoryMapStream(new CappedStream(stream, position,
                    length * 4));
                data = new Array<uint>(map.CreateUInt32(length), profile.DataProfile);
                stream.Seek(length * 4, SeekOrigin.Current);
            }

            var attributes = AttributesIndex.Deserialize(new LimitedStream(stream, stream.Position), true);

            return new MappedAttributesIndex(data, attributes);
        }

        /// <summary>
        /// Searches pointer of the given id, returns uint.maxvalue is no data was found.
        /// </summary>
        private uint Search(uint id, out int idx)
        {
            if (_reverseIndex != null)
            {
                if (_reverseIndex.TryGetValue(id, out idx))
                {
                    return _data[idx + 1];
                }
                return _noData;
            }

            if (_data == null ||
                _data.Length == 0)
            {
                idx = -1;
                return _noData;
            }

            // do binary search.
            var left = 0;
            var right = (_pointer - 2) / 2;
            var leftData = _data[left * 2];
            var rightData = _data[right * 2];

            if (leftData == id)
            {
                idx = left;
                return _data[left * 2 + 1];
            }
            if (rightData == id)
            {
                idx = right;
                return _data[right * 2 + 1];
            }

            while (left < right)
            {
                var middle = (left + right) / 2;
                var middleData = _data[middle * 2];

                if (right - left == 1)
                {
                    break; // id doesn't exist.
                }
                if (id < middleData)
                {
                    right = middle;
                }
                else if (id > middleData)
                {
                    left = middle;
                }
                else
                {
                    idx = middle;
                    return _data[middle * 2 + 1];
                }
            }

            idx = -1;
            return _noData;
        }

        /// <summary>
        /// Adds a new id-attributeId pair.
        /// </summary>
        private void Add(uint id, uint attributeId)
        {
            if (_reverseIndex == null)
            {
                throw new InvalidOperationException(
                    $"Cannot add new id's to a readonly MappedAttributesIndex, only update existing data. Make index writable again first: {id} not found.");
            }
            else
            {
                if (_data.Length <= _pointer + 2)
                {
                    _data.Resize(_data.Length + _blockSize);
                }

                _reverseIndex[id] = _pointer + 0;
                _data[_pointer + 0] = id;
                _data[_pointer + 1] = attributeId;

                _pointer += 2;
            }
        }
    }
}