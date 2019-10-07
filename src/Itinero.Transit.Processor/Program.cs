using System;
using System.Collections.Generic;
using System.Globalization;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            // enable logging.
            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine("[{0}-{3}] {1} - {2}", origin, level, message,
                    DateTime.Now.ToString(CultureInfo.InvariantCulture));
            };

            Log.Information("Starting Transit Data Preprocessor 0.1");

            // register switches.

            if (args.Length == 0)
            {
                Console.WriteLine("No arguments were given.");
                args = new[] {"--help"};
            }

            List<(DocumentedSwitch, Dictionary<string, string>)> switches;
            try
            {
                switches = SwitchParsers.ParseSwitches(args);


                var generatorOrModifier = 0;
                var sinks = 0;
                foreach (var (swtch, _) in switches)
                {
                    if (swtch is ITransitDbModifier || swtch is ITransitDbSource)
                    {
                        generatorOrModifier++;
                    }

                    if (swtch is ITransitDbSink)
                    {
                        sinks++;
                    }
                }

                if (generatorOrModifier == 0)
                {
                    throw new ArgumentException(
                        "Not a single switch generates or modifies a transitDB - you'll wont have a lot of output this way...");
                }

                if (sinks == 0)
                {
                    throw new ArgumentException(
                        "Not a single switch does something with the transitDB - perhaps you forgot '--write outputfile' or similar?");
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return;
            }


            TransitDb tdb;
            if (switches[0].Item1 is ITransitDbSource source)
            {
                tdb = source.Generate(switches[0].Item2);
                switches = switches.GetRange(1, switches.Count - 1);
            }
            else
            {
                tdb = new TransitDb(0);
            }


            foreach (var (swtch, parameters) in switches)
            {
                if (swtch is ITransitDbModifier modif)
                {
                    tdb = modif.Modify(parameters, tdb);
                    continue;
                }

                if (swtch is ITransitDbSink sink)
                {
                    sink.Use(parameters, tdb);
                    continue;
                }

                if (swtch is ITransitDbSource)
                {
                    throw new ArgumentException("A generator can only be the first argument");
                }

                throw new ArgumentException("Unknown switch type: " + swtch.Names[0]);
            }
        }
    }
}