using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchSelectStopById : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names =
            {"--select-stop", "--select-stops", "--filter-stop", "--filter-stops"};

        private static string About =
            "Filters the transit-db so that only stops with the given id(s) are kept. " +
            "All connections containing a removed location will be removed as well.\n\n" +
            "This switch is mainly used for fancy statistics.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("id", "ids", "The ';'-separated stops that should be kept"),

                    SwitchesExtensions.opt("allow-empty-connections",
                            "If flagged, the program will not crash if no connections are retained")
                        .SetDefault("false")
                };

        private const bool IsStable = true;

        public SwitchSelectStopById() :
            base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)

        {
            var ids = arguments["id"].Split(";").ToHashSet();
            var allowEmptyCon = bool.Parse(arguments["allow-empty-connections"]);


            foreach (var id in ids)
            {
                if (!old.Latest.StopsDb.SearchId(id, out _))
                {
                    throw new ArgumentException($"The global id {id} was not found");
                }
            }

            return old.Copy(allowEmptyCon,
                keepStop: stop => ids.Contains(stop.GlobalId),
                keepTrip: _ => false,
                keepConnection: x =>
                {
                    var stopIdMapping = x.reverseStopIdMapping;
                    var c = x.c;

                    if (stopIdMapping.TryGetValue(c.DepartureStop, out var departureGlobalId))
                    {
                        if (ids.Contains(departureGlobalId))
                        {
                            // departure is sought for
                            return true;
                        }
                    }

                    if (stopIdMapping.TryGetValue(c.ArrivalStop, out var arrivalGlobalId))
                    {
                        if (ids.Contains(arrivalGlobalId))
                        {
                            // departure is sought for
                            return true;
                        }
                    }

                    return false;
                }
            );
        }
    }
}