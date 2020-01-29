using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Itinero.Transit.Tests.Dummies
{
    internal class DummyStopsDb : IStopsDb
    {
        public IEnumerator<Stop> GetEnumerator()
        {
            throw new Exception();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(StopId id, out Stop t)
        {
            t = new Stop("dummyStop", (0,0));
            return true;
        }

        public bool TryGetId(string globalId, out StopId id)
        {
            throw new Exception();
        }

        public IEnumerable<uint> DatabaseIds { get; }

        public IStopsDb Clone()
        {
            throw new Exception();
        }

        public long Count => throw new Exception();

        public ILocationIndexing<Stop> LocationIndex { get; }
        public void PostProcess(uint zoomLevel)
        {
            throw new Exception();
        }

        public List<Stop> GetInRange((double lon, double lat) c, uint maxDistanceInMeter)
        {
            throw new Exception();
        }

        public void PostProcess()
        {
        }
    }
}