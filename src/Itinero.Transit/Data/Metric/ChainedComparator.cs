namespace Itinero.Transit.Journeys
{
    public  class ChainedComparator<T> : MetricComparator<T>
        where T : IJourneyMetric<T>
    {

        private readonly MetricComparator<T> _firstComparator, _spillOver;

        public ChainedComparator(MetricComparator<T> firstComparator, MetricComparator<T> spillOver)
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