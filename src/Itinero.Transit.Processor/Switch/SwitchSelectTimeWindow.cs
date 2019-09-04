using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchSelectTimeWindow : DocumentedSwitch, ITransitDbModifier
    {
        private static readonly string[] _names = {"--select-time", "--filter-time"};

        private static string _about =
            "Filters the transit-db so that only connections departing in the specified time window are kept. " +
            "This allows to take a small slice out of the transitDB, which can be useful to debug. " +
            "All locations will be kept.";


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
                    SwitchesExtensions.opt("allow-empty", "If flagged, the program will not crash if no connections are retained")
                        .SetDefault("false")
                };

        private const bool _isStable = true;


        public SwitchSelectTimeWindow
            () :
            base(_names, _about, _extraParams, _isStable)
        {
        }

        public void Modify(Dictionary<string, string> arguments, TransitDb transitDb)
        {
            var start = DateTime.ParseExact(arguments["window-start"], "yyyy-MM-dd_HH:mm:ss", null);
            start = start.ToUniversalTime();
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
                duration = (int) (endDate - start).TotalSeconds;
            }
            var allowEmpty = bool.Parse(arguments["allow-empty"]);



            endDate = start.AddSeconds(duration);
            var end = endDate.ToUnixTime();
            var old = transitDb;

            var filtered = new TransitDb(0);
            var wr = filtered.GetWriter();


            var stopIdMapping = new Dictionary<StopId, StopId>();

            var stops = old.Latest.StopsDb.GetReader();
            while (stops.MoveNext())
            {
                var newId = wr.AddOrUpdateStop(stops.GlobalId, stops.Longitude, stops.Latitude, stops.Attributes);
                var oldId = stops.Id;
                stopIdMapping.Add(oldId, newId);
            }


            var connsEnumerator = old.Latest.ConnectionsDb.GetDepartureEnumerator();
            connsEnumerator.MoveTo(start.ToUnixTime());
            var c = new Connection();
            var copied = 0;
            
            while (connsEnumerator.MoveNext() && connsEnumerator.CurrentDateTime <= end)
            {
                connsEnumerator.Current(c);
                wr.AddOrUpdateConnection(c);

                copied++;
            }


            wr.Close();


            if (!allowEmpty && copied == 0)
            {
                throw new Exception("There are no connections in the given timeframe.");
            }


            Console.WriteLine($"There are {copied} connections in the filtered transitDB");
        }
    }
}