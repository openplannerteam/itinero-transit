using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Filter
{
    internal class SelectTrip : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--select-trip", "--filter-trip"};

        private static string About =
            "Removes all connections and all stops form the database, except those of the specified trip ";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("id",
                        "The URI identifying the trip you want to keep")
                };

        private const bool IsStable = true;

        public SelectTrip() : base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDbSnapShot Modify(Dictionary<string, string> arguments, TransitDbSnapShot old)
        {
            var id = arguments["id"];

            return old.Copy(
                keepStop: _ => false,
                keepTrip: t => t.GlobalId.Equals(id),
                keepConnection: x =>
                {
                    var (_, _, tripIdMapping, _, connection) = x;
                    if (tripIdMapping.TryGetValue(id, out var tripId))
                    {
                        return connection.TripId.Equals(tripId);
                    }

                    return false;
                }
            );
        }
    }
}