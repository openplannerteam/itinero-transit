using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional
{
    public abstract class DefaultFunctionalTest :
        FunctionalTest<Boolean, (ConnectionsDb connections, StopsDb stops,
            string departureStopId, string arrivalStopId, DateTime departureTime, DateTime arrivalTime)>
    {
        
    }
}