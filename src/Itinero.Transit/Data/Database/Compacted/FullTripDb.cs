using Itinero.Transit.Data.Simple;

namespace Itinero.Transit.Data.Compacted
{
    
    public class FullTripDb :SimpleDb<FullTripId, FullTrip>, IDatabaseReader<FullTripId, FullTrip>
    {
        public FullTripDb(uint dbId) : base(dbId)
        {
        }

        public FullTripDb(SimpleDb<FullTripId, FullTrip> copyFrom) : base(copyFrom)
        {
        }
    }
}