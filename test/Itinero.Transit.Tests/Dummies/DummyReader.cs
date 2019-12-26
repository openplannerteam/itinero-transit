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
            throw new NotImplementedException();
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

        public bool SearchId(string globalId, out StopId id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<uint> DatabaseIds { get; }

        public IStopsDb Clone()
        {
            throw new NotImplementedException();
        }

        public ILocationIndexing<Stop> LocationIndex { get; }
        public void PostProcess(uint zoomLevel)
        {
            throw new NotImplementedException();
        }

        public List<Stop> GetInRange((double lon, double lat) c, uint maxDistanceInMeter)
        {
            throw new NotImplementedException();
        }

        public void PostProcess()
        {
            throw new NotImplementedException();
        }
    }
}