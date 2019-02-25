using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A default profile.
    /// </summary>
    public class DefaultProfile : Profile<TransferStats>
    {
        /// <summary>
        /// Creates a profile with default settings.
        /// </summary>
        /// <param name="snapshot"></param>
        public DefaultProfile(TransitDb.TransitDbSnapShot snapshot)
        : base(new InternalTransferGenerator(180), 
            new CrowsFlightTransferGenerator(snapshot, maxDistance: 500 /*meter*/,  speed: 1.4f /*meter/second*/),
            TransferStats.Factory,
            TransferStats.ProfileTransferCompare)
        {
            
        }
    }
}