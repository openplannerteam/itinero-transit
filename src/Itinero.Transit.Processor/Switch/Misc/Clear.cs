using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Misc
{
    internal class Clear : DocumentedSwitch, IMultiTransitDbModifier
    {
        private static readonly string[] _names = {"--clear"};

        private static string About =
            "Removes the currently loaded database from memory. This switch is only useful in interactive shell sessions";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable =true;

        public Clear() : base(_names,About,_extraParams,IsStable)
        {}



        public IEnumerable<TransitDb> Modify(Dictionary<string, string> __, List<TransitDb> _)
        {
            return new List<TransitDb>();
        }
    }
}