namespace Itinero.Transit.Data
{
    public interface IConnectionReader : IConnection
    {
        bool MoveTo(uint dbId, uint connectionId);
    }
}