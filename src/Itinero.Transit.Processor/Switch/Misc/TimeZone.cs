using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch.Misc
{
    internal class ShowTimeZone : DocumentedSwitch, IMultiTransitDbModifier, IMultiTransitDbSource, IMultiTransitDbSink
    {
        private static readonly string[] _names = {"--timezone"};

        private static string About =
            "Shows the current timezone of the machine.";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("time", "A date to test parsing")
                        .SetDefault("now")
                };

        private const bool IsStable = true;

        public ShowTimeZone() : base(_names, About, _extraParams, IsStable)
        {
        }

        private void Run(Dictionary<string, string> parameters)
        {
            var t = ParseDate(parameters["time"]);
            Console.WriteLine($"The given time is {t:s}");
            Console.WriteLine($"The given time in UTC is {t.ToUniversalTime():s}");
            Console.WriteLine($"The UNIX-timestamp is {t.ToUniversalTime().ToUnixTime()}"); 
            var tzinfo = TimeZoneInfo.Local;
            Console.WriteLine($"The timezone of this machine is {tzinfo.Id}, the offset is currently {tzinfo.BaseUtcOffset} ({tzinfo}). The offset can change during summer/wintertime");
        }
        

        public IEnumerable<TransitDb> Modify(Dictionary<string, string> parameters, List<TransitDb> tdbs)
        {
            Run(parameters);
            return tdbs;
        }

        public IEnumerable<TransitDb> Generate(Dictionary<string, string> parameters)
        {
            Run(parameters);
            return new List<TransitDb>();
        }

        public void Use(Dictionary<string, string> parameters, IEnumerable<TransitDbSnapShot> transitDbs)
        {
            Run(parameters);
        }
    }
}