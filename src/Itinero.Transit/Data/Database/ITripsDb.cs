using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface ITripsDb : IDatabaseReader<TripId, Trip>, IClone<ITripsDb>
    {
        void PostProcess();
        
        long Count { get; }
    }
}