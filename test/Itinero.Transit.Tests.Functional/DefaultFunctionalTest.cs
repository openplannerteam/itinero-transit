using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional
{
    public abstract class DefaultFunctionalTest<T> :
        FunctionalTest<bool, WithTime<T>> where T : IJourneyMetric<T>
    {
    }
}