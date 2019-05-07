namespace Itinero.Transit.Data
{
    public interface ITripReader : ITrip
    {
        bool MoveTo(TripId tripId);
    }
}