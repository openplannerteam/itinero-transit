using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Processor.Validator;

namespace Itinero.Transit.Processor.Switch
{
    internal class SwitchValidate : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--validate"};

        private static string _about =
            "Checks assumptions on the database, e.g: are the coordinates of stops within the correct range? Does the train not drive impossibly fast? Are there connections going back in time?";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("type",
                            "Only show messages of this type. Multiple are allowed if comma-separated. Note: the totals will still be printed")
                        .SetDefault("*"),
                    SwitchesExtensions.opt("cutoff", "Only show this many messages. Default: 25")
                        .SetDefault("10"),
                    SwitchesExtensions.opt("relax", "Use more relaxed parameters for real-world data, if they should not be a problem for journey planning. For example, teleportations <10km are ignored, very fast trains <10km are ignored. Notice that I would expect those to cases to cause regular delays though!")
                        .SetDefault("false")
                };

        private const bool _isStable = true;

        public SwitchValidate() : base(_names, _about, _extraParams, _isStable)
        {
        }


        private List<IValidation> Validators = new List<IValidation>
        {
            new ValidateTrips()
        };


        public void Use(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            var cutoff = parameters.Int("cutoff");
            var typesToPrint = parameters["type"];
            var relax = parameters.Bool("relax");

            foreach (var validation in Validators)
            {
                var msgs = validation.Validate(transitDb, relax);

                if (msgs.Count == 0)
                {
                    Console.WriteLine("No errors or warnings in validation. This is nearly impossible!");
                }
                var hist = msgs.CountTypes();

                if (string.IsNullOrWhiteSpace(typesToPrint))
                {
                    typesToPrint = "*";
                }

                var toPrint = typesToPrint.Equals("*") ? hist.Keys.ToList() : typesToPrint.Split(",").ToList();

                foreach (var type in toPrint)
                {
                    if (!hist.ContainsKey(type))
                    {
                        Console.WriteLine($"Type {type} not found in histogram. Try one of {string.Join(",", hist.Keys)}");
                    }

                    var count = hist[type];
                    msgs.PrintType(type,  (int) count.count, cutoff);
                }

                foreach (var (type, (count, isHardError)) in hist)
                {
                    var err = isHardError ? "error" : "warning";
                    if (count != 1)
                    {
                        err += "s";
                    }
                    Console.WriteLine($"Found {count} {err} of type {type}");
                }
                
            }
        }
    }
}