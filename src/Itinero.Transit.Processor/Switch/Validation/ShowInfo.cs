using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Validation
{
    class ShowInfo : DocumentedSwitch, IMultiTransitDbSink
    {
        private static readonly string[] _names = {"--show-info", "--info"};

        private static string About =
            "Dumps all the metadata of the currently loaded database";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;


        public ShowInfo() : base(_names, About, _extraParams, IsStable)
        {
        }

        public void Use(Dictionary<string, string> parameters, IEnumerable<TransitDbSnapShot> transitDbs)
        {
            foreach (var transitDb in transitDbs)
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
}