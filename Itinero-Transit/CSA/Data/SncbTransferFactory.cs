namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Creates a transfer object according to the SNCB-policy
    /// </summary>
    public class SncbTransferFactory
    {
        private readonly int _defaultSeconsNeeded;

        public SncbTransferFactory(int defaultSeconsNeeded)
        {
            _defaultSeconsNeeded = defaultSeconsNeeded;
        }

        public SncbTransferFactory() : this(3 * 60)
        {
        }

        /// <summary>
        /// Create a transfer through a SNCB-station from one train to another.
        ///
        /// For now, a simple 'transfer-connection' is created. In the future, more advanced connections can be used
        /// (e.g. with instructions through the station...)
        ///
        /// Returns null if the transfer can't be made (transfertime is not enough)
        /// Returns connection 'to' if the connection is on the same trip
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IConnection CreateTransfer(IConnection from, IConnection to)
        {
            return null;


        }
    }
}