using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    internal static class SwitchExecutor
    {
        
        /// <summary>
        /// Applies the given switch on the list of transitDbs;
        /// Returns a new list
        /// </summary>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static IEnumerable<TransitDb> ApplySwitch(
            this IEnumerable<TransitDb> tdbs,
            (DocumentedSwitch swtch, Dictionary<string, string> @parameters) switchAndParams)
        {
            var (swtch, parameters) = switchAndParams;
            switch (swtch)
            {
                case IMultiTransitDbModifier multiModifier:
                    return multiModifier.Modify(parameters, tdbs.ToList());
                case ITransitDbModifier modifier:
                    return tdbs.Select(tdb => modifier.Modify(parameters, tdb)).ToList();
                
                
                case IMultiTransitDbSource source:
                    return tdbs.Concat(source.Generate(parameters)).ToList();
                case ITransitDbSource source:
                    var newTdb = source.Generate(parameters);
                    return tdbs.Concat(new List<TransitDb> {newTdb}).ToList();
                
                
                case IMultiTransitDbSink multiSink:
                    multiSink.Use(parameters, tdbs.Select(tdb => tdb.Latest));
                    return tdbs;
                case ITransitDbSink sink:
                    foreach (var tdb in tdbs)
                    {
                        sink.Use(parameters, tdb.Latest);
                    }
                    return tdbs;
                
                default:
                    throw new ArgumentException("Unknown switch type: " + swtch.Names[0]);
            }
        }
    }
}