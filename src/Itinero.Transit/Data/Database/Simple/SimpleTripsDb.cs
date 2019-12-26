using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Simple
{
    public class SimpleTripsDb : SimpleDb<TripId, Trip>, ITripsDb, IClone<SimpleTripsDb>
    {
        public SimpleTripsDb(uint dbId) : base(dbId)
        {
        }

        public SimpleTripsDb(SimpleDb<TripId, Trip> copyFrom) : base(copyFrom)
        {
        }

        public void PostProcess()
        {
        }

        public SimpleTripsDb Clone()
        {
            return new SimpleTripsDb(this);
        }


        ITripsDb IClone<ITripsDb>.Clone()
        {
            return Clone();
        }
    }
}