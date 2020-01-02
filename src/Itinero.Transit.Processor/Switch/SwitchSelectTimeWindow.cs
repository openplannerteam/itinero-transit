using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchSelectTimeWindow : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--select-time", "--filter-time"};

        private static string About =
            "Filters the transit-db so that only connections departing in the specified time window are kept. " +
            "This allows to take a small slice out of the transitDB, which can be useful to debug. " +
            "Only used locations will be kept.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("window-start", "start",
                        "The start time of the window, specified as `YYYY-MM-DD_hh:mm:ss` (e.g. `2019-12-31_23:59:59`)"),
                    SwitchesExtensions.obl("duration", "window-end",
                        "Either the length of the time window in seconds or the end of the time window in `YYYY-MM-DD_hh:mm:ss`"),
                    //   opt("interpretation",
                    //           "How the departure times are interpreted. Options are: `actual`, `planned` or `both`. If `planned` is specified, the connection will only be kept if the planned departure time is within the window (thus as if there would not have been a delay). With `actual`, only the actual (with delays) departure time is used. Both will keep the connection if either the actual or planned departure time are within the window.")
                    //      .SetDefault("both"),
                    SwitchesExtensions.opt("allow-empty",
                            "If flagged, the program will not crash if no connections are retained")
                        .SetDefault("false")
                };

        private const bool IsStable = true;


        public SwitchSelectTimeWindow
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }

        public TransitDb Modify(Dictionary<string, string> arguments, TransitDb old)
        {
            var startDate = DateTime.ParseExact(arguments["window-start"], "yyyy-MM-dd_HH:mm:ss", null);
            startDate = startDate.ToUniversalTime();
            int duration;
            DateTime endDate;
            try
            {
                duration = int.Parse(arguments["duration"]);
            }
            catch (FormatException)
            {
                endDate = DateTime.ParseExact(arguments["duration"], "yyyy-MM-dd_HH:mm:ss", null);
                endDate = endDate.ToUniversalTime();
                duration = (int) (endDate - startDate).TotalSeconds;
            }

            var allowEmpty = bool.Parse(arguments["allow-empty"]);

            endDate = startDate.AddSeconds(duration);
            var end = endDate.ToUnixTime();

            var start = startDate.ToUnixTime();

            return old.Copy(
                allowEmpty,
                keepStop: _ => false,
                keepTrip: _ => false,
                keepConnection: x =>
                {
                   var t= x.c.DepartureTime;
                   return start <= t && t < end;
                }
            );
        }
    }
}