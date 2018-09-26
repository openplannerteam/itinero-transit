namespace Itinero_Transit.CSA
{
    public  class ChainedComparator<T> : IStatsComparator<T>
    {

        private readonly IStatsComparator<T> _firstComparator, _spillOver;

        public ChainedComparator(IStatsComparator<T> firstComparator, IStatsComparator<T> spillOver)
        {
            _firstComparator = firstComparator;
            _spillOver = spillOver;
        }

        public int ADominatesB(T a, T b)
        {
            var value = _firstComparator.ADominatesB(a, b);
            return value == 0 ? _spillOver.ADominatesB(a, b) : value;
        }
        
        
    }
}