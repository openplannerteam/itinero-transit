using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Misc
{
    internal class Merge : DocumentedSwitch, IMultiTransitDbModifier
    {
        private static readonly string[] _names = {"--merge"};

        private static string About =
            "Merges all of the currently loaded transitdbs into a single transitdb";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = true;

        public Merge() : base(_names, About, _extraParams, IsStable)
        {
        }


        public IEnumerable<TransitDb> Modify(Dictionary<string, string> parameters, List<TransitDb> tdbs)
        {
            return new List<TransitDb> {TransitDb.CreateMergedTransitDb(tdbs.Select(tdb => tdb.Latest))};
        }
    }
}