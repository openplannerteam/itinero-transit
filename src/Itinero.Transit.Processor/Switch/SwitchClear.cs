using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchClear : DocumentedSwitch,
        ITransitDbModifier
    {
        private static readonly string[] _names = {"--clear"};

        private static string _about =
            "Removes the currently loaded database from memory. This switch is only useful in interactive shell sessions";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool _isStable =true;

        public SwitchClear() : base(_names,_about,_extraParams,_isStable)
        {}


        public TransitDb Modify(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            return new TransitDb(0);
        }
    }
}