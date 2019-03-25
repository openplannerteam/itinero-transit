using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleLiteral

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
        public DefaultProfile()
        : base(new InternalTransferGenerator(180), 
            new CrowsFlightTransferGenerator(maxDistance: 500 /*meter*/,  speed: 1.4f /*meter/second*/),
            TransferStats.Factory,
            TransferStats.ProfileTransferCompare)
        {
            
        }
    }
}