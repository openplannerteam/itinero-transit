using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    public interface ITransitDbSource
    {
        TransitDbSnapShot Generate(Dictionary<string, string> parameters);
    }

    public interface IMultiTransitDbSource
    {
        List<TransitDbSnapShot> Generate(Dictionary<string, string> parameters);
    }

    public interface IMultiTransitDbSink
    {
        void Use(Dictionary<string, string> parameters, List<TransitDbSnapShot> transitDbs);
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
        TransitDbSnapShot Modify(Dictionary<string, string> parameters, TransitDbSnapShot transitDb);
    }

    public interface IMultiTransitDbModifier
    {
        /// <summary>
        /// Modifies the transitdbs
        /// </summary>
        List<TransitDbSnapShot> Modify(Dictionary<string, string> parameters, List<TransitDbSnapShot> transitDbs);
    }
}