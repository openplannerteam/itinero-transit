using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    internal class SwitchUnusedFilter : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--filter-unused", "--remove-unused", "--rm-unused"};

        private static string About ="Removes stops and trips without connections.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams = new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;

        public SwitchUnusedFilter() : base(_names, About, _extraParams, IsStable)
        {
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)

        {
            var newDb = old.Copy(
                keepStop: _ => false,
                keepTrip: _ => false,
                keepConnection: _ => true);

            var newStopsCount = newDb.Latest.StopsDb.Count();
            Console.WriteLine($"There are {newStopsCount} stops (removed {old.Latest.StopsDb.Count() - newStopsCount})");
            return newDb;
        }
    }
}