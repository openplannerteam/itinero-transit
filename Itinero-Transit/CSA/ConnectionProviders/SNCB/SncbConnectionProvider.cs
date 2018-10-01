using System;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public class SncbConnectionProvider : IConnectionsProvider
    {
        
        public static readonly string Irail = "http://graph.irail.be/sncb/connections?departureTime=";

        
        private readonly int _transferSecondsNeeded;

        public SncbConnectionProvider(int transferSecondsNeeded)
        {
            _transferSecondsNeeded = transferSecondsNeeded;
        }

        public SncbConnectionProvider() : this(3 * 60)
        {
            
        }


        public Uri TimeTableIdFor(DateTime time)
        {
            time = time.AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
            return new Uri($"{Irail}{time:yyyy-MM-ddTHH:mm:ss}.000Z");
        }

        public IConnection GetConnection(Uri id)
        {
            var c = new SncbConnection(id);
            c.Download();
            return c;
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            var tt = new SncbTimeTable(id);
            tt.Download();
            return tt;
        }

        /// <inheritdoc />
        ///  <summary>
        ///  Create a transfer through a SNCB-station from one train to another.
        ///  For now, a simple 'transfer-connection' is created. In the future, more advanced connections can be used
        ///  (e.g. with instructions through the station...)
        ///  Returns null if the transfer can't be made (transfertime is not enough)
        ///  Returns connection 'to' if the connection is on the same trip
        ///  </summary>
        ///  <param name="from"></param>
        ///  <param name="to"></param>
        ///  <returns></returns>
        public IConnection CalculateInterConnection(IConnection @from, IConnection to)
        {
            if ((to.DepartureTime() - from.ArrivalTime()).TotalSeconds < _transferSecondsNeeded)
            {
                // To little time to make the transfer
                return null;
            }

            return new InternalTransfer(to.DepartureLocation(), to.Operator(), from.ArrivalTime(),
                from.ArrivalTime().AddSeconds(_transferSecondsNeeded));
        }
    }
}