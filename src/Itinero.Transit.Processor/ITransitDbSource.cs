using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor
{
    public interface ITransitDbSource
    {
        TransitDb Generate(Dictionary<string, string> parameters);
    }
    
    public interface ITransitDbSink
    {
        /// <summary>
        /// Does _not_ change the transitDb
        /// </summary>
        void Use(Dictionary<string, string> parameters, TransitDb transitDb);
    }
    public interface ITransitDbModifier
    {
        /// <summary>
        /// Modifies the transitdb
        /// </summary>
        void Modify(Dictionary<string, string> parameters, TransitDb transitDb);
    }
}