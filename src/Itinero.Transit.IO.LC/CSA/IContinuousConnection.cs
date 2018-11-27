using System;

namespace Itinero.Transit
{
    /// <summary>
    /// A ContinuousConnection is a connection that can be moved in time.
    /// Walking is the textbook example: if the traveller starts their walk a few minutes later,
    /// the traveller will arrive a few minutes later as well.
    ///
    /// This is opposed to most public transport connections (which are discrete by default):
    /// If the traveller is a few minutes late, they will have missed their connection and have to wait e.g. another hour.
    ///
    /// Due to the framework, a continuous connection has a departure- and arrivalTime too.
    /// However, they can be moved around freely with `MoveTime`
    /// 
    /// </summary>
    public interface IContinuousConnection : IConnection
    {

        /// <summary>
        /// Adds this number of seconds to both arrival- and departuretime
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        IContinuousConnection MoveTime(double seconds);

        IContinuousConnection MoveDepartureTime(DateTime newDepartureTime);

    }
}