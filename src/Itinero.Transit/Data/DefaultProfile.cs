using Itinero.Transit.Algorithms.Filter;
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
        public DefaultProfile(uint maxSearch = 500)
        : base(new InternalTransferGenerator(180), 
            new CrowsFlightTransferGenerator(maxDistance: maxSearch /*meter*/,  speed: 1.4f /*meter/second*/),
            TransferMetric.Factory,
            TransferMetric.ParetoCompare,
            connectionFilter:new CancelledConnectionFilter())
        {
            
        }
    }
}