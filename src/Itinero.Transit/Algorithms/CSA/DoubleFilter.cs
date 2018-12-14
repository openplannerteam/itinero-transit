using Itinero.Transit.Data;

namespace Itinero.Transit.Algorithms.CSA
{
    public class DoubleFilter : IConnectionFilter
    {
        private readonly IConnectionFilter[] _filters;

        public DoubleFilter(params IConnectionFilter[] filters)
        {
            _filters = filters;
        }
        
        public bool CanBeTaken(IConnection c)
        {
            foreach (var f in _filters)
            {
                if (!f.CanBeTaken(c))
                {
                    return false;
                }
            }
            return true;
        }
        

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            foreach (var f in _filters)
            {
                f.CheckWindow(depTime, arrTime);
            }
        }
    }
}