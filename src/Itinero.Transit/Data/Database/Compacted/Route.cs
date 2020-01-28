using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Compacted
{
    public class Route : KeyList<StopId>, IGlobalId
    {
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }

        public Route(string globalId,
            IEnumerable<StopId> stops,
            IReadOnlyDictionary<string, string> attributes = null)
            : base(stops)
        {
            GlobalId = globalId;
            Attributes = attributes;
        }
    }
}