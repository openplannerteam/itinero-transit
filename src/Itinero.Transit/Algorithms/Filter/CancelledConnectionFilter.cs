using Itinero.Transit.Data;

namespace Itinero.Transit.Journey.Filter
{
    /// <summary>
    /// A filter which forbids to take cancelled connections
    /// </summary>
    public class CancelledConnectionFilter : IConnectionFilter
    {
        public bool CanBeTaken(Connection c)
        {
            return !c.IsCancelled();
        }

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            // Always valid, no matter what
        }
    }
}