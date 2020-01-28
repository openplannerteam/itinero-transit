using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Validation
{
    internal class RemoveUnused : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--remove-unused", "--filter-unused", "--rm-unused"};

        private static string About = "Removes stops and trips without connections.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams = new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;

        public RemoveUnused() : base(_names, About, _extraParams, IsStable)
        {
        }

        public TransitDbSnapShot Modify(Dictionary<string, string> arguments, TransitDbSnapShot old)

        {
            var newDb = old.Copy(
                keepStop: _ => false,
                keepTrip: _ => false,
                keepConnection: _ => true);

            var newStopsCount = newDb.Stops.Count();
            Console.WriteLine(
                $"There are {newStopsCount} stops (removed {old.Stops.Count() - newStopsCount})");
            return newDb;
        }
    }
}