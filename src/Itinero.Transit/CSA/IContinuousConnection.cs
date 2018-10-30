namespace Itinero.Transit.CSA
{
    public interface IContinuousConnection : IConnection
    {

        /// <summary>
        /// Adds this number of seconds to both arrival- and departuretime
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        IContinuousConnection MoveTime(double seconds);

    }
}