using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Itinero.Transit.Tests.Core
{
    internal class DummyReader : IStopsReader
    {
        public string GlobalId { get; }
        public StopId Id { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        public IAttributeCollection Attributes { get; }

        public HashSet<uint> DatabaseIndexes()
        {
            return new HashSet<uint>();
        }

        public bool MoveNext()
        {
            throw new Exception("Not implemented - should not be called");
        }

        public bool MoveTo(StopId stop)
        {
            return true;
        }

        public bool MoveTo(string globalId)
        {
            throw new Exception("Not implemented - should not be called");
        }

        public void Reset()
        {
            throw new Exception("Not implemented - should not be called");
        }

        public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            throw new Exception("Not implemented - should not be called");
        }
    }
}