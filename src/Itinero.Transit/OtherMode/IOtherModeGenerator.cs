using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

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
        /// Returns Max_Value if not possible or if this is not the responsibility (e.g. for a walk, if from == to).
        ///
        /// </summary>
        /// <returns></returns>
        uint TimeBetween(IStop from, IStop to);


        /// <summary>
        /// Gives the times needed to travel from this stop to all the given locations.
        /// This can be used to do time estimations.
        ///
        /// The target stop should not be included if travelling towards it is not possible.
        ///
        /// This method is used mainly for optimization.
        ///
        /// Warning: the enumerators in 'to' will often be a list of 'n' times the same object.
        /// However, calling 'MoveNext' will cause that object to change state.
        /// In other words, 'to' should always be used in a 'for-each' loop.
        /// </summary>
        Dictionary<StopId, uint> TimesBetween(IStop from,
            IEnumerable<IStop> to);

        /// <summary>
        /// The maximum range of this IOtherModeGenerator, in meters.
        /// This generator will only be asked to generate transfers within this range.
        /// If an stop out of this range is given to create a transfer,
        /// the implementation can choose to either return a valid transfer or to return null
        /// </summary>
        /// <returns></returns>
        float Range();


      
        /// <summary>
        /// An URL which represents this other mode generator.
        /// Can be used to deduct the otherModeGenerator used.
        /// This should be a constant.
        /// </summary>
        /// <returns></returns>
        string OtherModeIdentifier();

        /// <summary>
        /// Gives the actual OtherModeGenerator which will construct the route for this
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        IOtherModeGenerator GetSource(StopId from, StopId to);

    }
}