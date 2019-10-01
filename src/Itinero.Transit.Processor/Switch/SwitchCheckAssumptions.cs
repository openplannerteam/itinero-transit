using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Processor.Validator;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchCheckAssumptions : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--validate"};

        private static string _about =
            "Checks assumptions on the database, e.g: are the coordinates of stops within the correct range? Does the train not drive impossibly fast? Are there connections going back in time?";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("cutoff", "Only show this many messages. Default: 25")
                        .SetDefault("10")
                };

        private const bool _isStable = false;

        public SwitchCheckAssumptions() : base(_names, _about, _extraParams, _isStable)
        {
        }


        private List<IValidation> Validators = new List<IValidation>
        {
            new ValidateTrips()
        };


        public void Use(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            var cutoff = uint.Parse(parameters["cutoff"]);

            foreach (var validation in Validators)
            {
                var msgs = validation.Validate(transitDb);
                var hist = msgs.CountTypes();

                foreach (var (type, count) in hist)
                {
                    msgs.PrintType(type, (int) count, (int) cutoff);
                }
                
                foreach (var (type, count) in hist)
                {
                    Console.WriteLine($"Found {count} errors of type {type}");
                }

            }
        }
    }
}