namespace Itinero.Transit.Journey.Filter
{  /// <summary>
    /// A journey filter helps to optimize PCS by saying if a journey should be taken or not.
    ///
    /// For example, journeys with more then 5 number of transfers are often not preferred journeys.
    /// A journey filter decides on an entire journey if it should be taken or not.
    /// </summary>
    public interface IJourneyFilter<T> where T : IJourneyMetric<T>
    {

        bool CanBeTaken(Journey<T> journey);

    }
}