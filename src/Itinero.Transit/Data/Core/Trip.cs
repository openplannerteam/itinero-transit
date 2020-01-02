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
        public Dictionary<string, string> Attributes { get; }

        public Trip(string globalId)
        {
            GlobalId = globalId;
        }
        
        public Trip(string globalId, Dictionary<string, string> attributes)
        {
            GlobalId = globalId;
            Attributes = attributes;
        }
    }
}