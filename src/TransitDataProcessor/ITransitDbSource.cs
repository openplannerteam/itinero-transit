using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.DataProcessor
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
        /// <param name="transitDb"></param>
        void Use(Dictionary<string, string> parameters, TransitDb transitDb);
    }
    public interface ITransitDbModifier
    {
        /// <summary>
        /// Modifies the transitdb
        /// </summary>
        /// <param name="transitDb"></param>
        void Modify(Dictionary<string, string> parameters, TransitDb transitDb);
    }
}