using System;
using System.Collections.Generic;

namespace Itinero.Transit.Data.Core
{
    /// <summary>
    /// The class representing a single trip and related attributes.
    /// This can be rewritten and should not be shared amongst threads
    /// </summary>
    [Serializable]
    public class Trip : IGlobalId
    {
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }
        
        public OperatorId Operator { get; }

        public Trip(string globalId)
        {
            GlobalId = globalId;
        }
        
        public Trip(string globalId, IReadOnlyDictionary<string, string> attributes)
        {
            GlobalId = globalId;
            Attributes = attributes;
        }
    }
}