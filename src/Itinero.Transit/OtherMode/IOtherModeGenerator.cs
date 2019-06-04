using Itinero.Transit.Data;

namespace Itinero.Transit.OtherMode
{
    /// <summary>
    /// The transfergenerator takes a journey and a next connection.
    /// Using those, it extends the journey if this is possible.
    /// </summary>
    public interface IOtherModeGenerator
    {
    
        /// <summary>
        /// Gives the time needed to travel from this stop to the next.
        /// This can be used to do time estimations.
        ///
        /// Returns Max_Value if not possible
        /// </summary>
        /// <returns></returns>
        uint TimeBetween(IStopsReader reader, LocationId from, LocationId to);

        /// <summary>
        /// The maximum range of this IOtherModeGenerator, in meters.
        /// This generator will only be asked to generate transfers within this range.
        /// If an stop out of this range is given to create a transfer,
        /// the implementation can choose to either return a valid transfer or to return null
        /// </summary>
        /// <returns></returns>
        float Range();
    }
}