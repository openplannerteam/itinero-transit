using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Itinero.Transit.Tests.Core
{
    internal class DummyReader : IStopsReader
    {
        public string GlobalId { get; }
        public LocationId Id { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public IAttributeCollection Attributes { get; }

        public HashSet<uint> DatabaseIndexes()
        {
            return new HashSet<uint>();
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public bool MoveTo(LocationId stop)
        {
            return true;
        }

        public bool MoveTo(string globalId)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            throw new NotImplementedException();
        }
    }
}