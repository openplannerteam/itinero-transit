using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchShowInfo : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--show-info", "--info"};

        private static string About =
            "Dumps all the metadata of the currently loaded database";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;


        public SwitchShowInfo() : base(_names, About, _extraParams, IsStable)
        {
        }

        public void Use(Dictionary<string, string> parameters, TransitDbSnapShot transitDb)
        {
            var txt =
                $"# {transitDb.GlobalId}\n\n";
            foreach (var kv in transitDb.Attributes)
            {
                txt += $" - {kv.Key} = {kv.Value}\n";
            }
            Console.WriteLine(txt);
        }
    }
}