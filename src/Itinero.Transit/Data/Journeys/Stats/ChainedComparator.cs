namespace Itinero.Transit.Journeys
{
    public  class ChainedComparator<T> : StatsComparator<T>
        where T : IJourneyStats<T>
    {

        private readonly StatsComparator<T> _firstComparator, _spillOver;

        public ChainedComparator(StatsComparator<T> firstComparator, StatsComparator<T> spillOver)
        {
            _firstComparator = firstComparator;
            _spillOver = spillOver;
        }

        public override int ADominatesB(Journey<T> a, Journey<T> b)
        {
            var value = _firstComparator.ADominatesB(a, b);
            return value == 0 ? _spillOver.ADominatesB(a, b) : value;
        }
        
        
    }
}