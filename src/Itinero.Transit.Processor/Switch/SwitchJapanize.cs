using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchJapanize : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names =
            {"--undo-delays", "--japanize", "--the-dutch-are-better", "--swiss-perfection"};

        private static readonly string _about =
            "Removes all the delays of the trips, so recreate the planned schedule.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = true;


        public SwitchJapanize
            () :
            base(_names, _about, _extraParams, IsStable)
        {
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)
        {
            var delaySum = 0;

            var newDb = old.Copy(
                modifyConnection: c =>

                {
                    delaySum += c.DepartureDelay;
                    return new Connection(
                        c.GlobalId,
                        c.DepartureStop,
                        c.ArrivalStop,
                        c.DepartureTime,
                        c.TravelTime,
                        0, 0,
                        c.Mode, c.TripId);
                }
            );
            Console.WriteLine($"Removed {delaySum / 60} minutes of delay. If only it was that easy in Belgium too...");
            return newDb;
        }
    }
}