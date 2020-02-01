using System;
using System.Collections.Generic;

namespace Itinero.Transit.Data.Core
{
    [Serializable]
    public class Operator : IGlobalId
    {
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }

        public Operator(string globalId)
        {
            GlobalId = globalId;
        }
        
        public Operator(string globalId, IReadOnlyDictionary<string, string> attributes)
        {
            GlobalId = globalId;
            Attributes = attributes;
        }
    }
}