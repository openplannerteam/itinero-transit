using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    public interface ITransitDbSource
    {
        TransitDb Generate(Dictionary<string, string> parameters);
    }

    public interface IMultiTransitDbSource
    {
        IEnumerable<TransitDb> Generate(Dictionary<string, string> parameters);
    }

    public interface IMultiTransitDbSink
    {
        void Use(Dictionary<string, string> parameters, IEnumerable<TransitDbSnapShot> transitDbs);
    }

    public interface ITransitDbSink
    {
        /// <summary>
        /// Does _not_ change the transitDb
        /// </summary>
        void Use(Dictionary<string, string> parameters, TransitDbSnapShot transitDb);
    }

    public interface ITransitDbModifier
    {
        /// <summary>
        /// Modifies the transitdb
        /// </summary>
        TransitDb Modify(Dictionary<string, string> parameters, TransitDb transitDb);
    }

    public interface IMultiTransitDbModifier
    {
        /// <summary>
        /// Modifies the transitdbs
        /// </summary>
        IEnumerable<TransitDb> Modify(Dictionary<string, string> parameters, List<TransitDb> transitDbs);
    }
}