using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional
{
    public abstract class DefaultFunctionalTest :
        FunctionalTest<bool, (TransitDb transitDb,
            string departureStopId, string arrivalStopId, 
            DateTime departureTime, DateTime arrivalTime)>
    {
        
    }
}