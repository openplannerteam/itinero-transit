using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleLiteral

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A default profile.
    /// </summary>
    public class DefaultProfile : Profile<TransferMetric>
    {
        /// <summary>
        /// Creates a profile with default settings.
        /// </summary>
        public DefaultProfile()
        : base(new InternalTransferGenerator(180), 
            new CrowsFlightTransferGenerator(maxDistance: 500 /*meter*/,  speed: 1.4f /*meter/second*/),
            TransferMetric.Factory,
            TransferMetric.ProfileTransferCompare)
        {
            
        }
    }
}