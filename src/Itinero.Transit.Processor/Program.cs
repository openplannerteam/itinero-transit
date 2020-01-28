using System;
using System.Collections.Generic;
using System.Globalization;
using Itinero.Transit.Data;
using Itinero.Transit.Processor.Switch;
using OsmSharp.Logging;

namespace Itinero.Transit.Processor
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // enable logging.
            Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine("[{0}-{3}] {1} - {2}", origin, level, message,
                    DateTime.Now.ToString(CultureInfo.InvariantCulture));
            };

            if (args.Length == 0)
            {
                Console.WriteLine("No arguments were given.");
                args = new[] {"--help"};
            }

            List<(DocumentedSwitch, Dictionary<string, string>)> switches;
            try
            {
                switches = SwitchParsers.ParseSwitches(args);
                ValidateSwitches(switches);

                var tdbs = new List<TransitDbSnapShot>();
                foreach (var sw in switches)
                {
                    tdbs = tdbs.ApplySwitch(sw);
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
#if DEBUG
                throw;
#endif
            }
        }

        private static void ValidateSwitches(IEnumerable<(DocumentedSwitch, Dictionary<string, string>)> switches)
        {
            var generatorOrModifier = 0;
            var sinks = 0;
            foreach (var (swtch, _) in switches)
            {
                if (swtch is ITransitDbModifier || swtch is ITransitDbSource || swtch is IMultiTransitDbModifier ||
                    swtch is IMultiTransitDbSource)
                {
                    generatorOrModifier++;
                }

                if (swtch is ITransitDbSink || swtch is IMultiTransitDbSink)
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
    }
}