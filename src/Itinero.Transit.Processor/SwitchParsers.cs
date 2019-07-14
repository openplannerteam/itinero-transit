using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Processor.Switch;

namespace Itinero.Transit.Processor
{
    /// <summary>
    /// A switch parser.
    /// </summary>
    static class SwitchParsers
    {
        // The help function does print in the same order as here
        // so put the most important switches up top
        public static List<(string category, List<DocumentedSwitch> swtch)> Documented =
            new List<(string category, List<DocumentedSwitch> swtch)>
            {
                ("Creating a transitdb", new List<DocumentedSwitch>
                {
                    new SwitchCreateTransitDbLC(),
                    new SwitchCreateTransitDbOSM()
                }),

                ("Filtering to and from file", new List<DocumentedSwitch>
                {
                    new SwitchSelectTimeWindow(),
                    new SwitchSelectStops()
                }),

                ("Saving to and from file", new List<DocumentedSwitch>
                {
                    new SwitchReadTransitDb(),
                    new SwitchDumpTransitDbStops(),
                    new SwitchDumpTransitDbConnections(),
                    new SwitchWriteTransitDb(),
                }),

                ("Misc", new List<DocumentedSwitch>
                    {new HelpSwitch()})
            };


        /// <summary>
        /// Returns true if this argument is a switch.
        /// </summary>
        private static bool IsSwitch(string name)
        {
            return name.StartsWith("--");
        }

        /// <summary>
        /// Finds a switch by it's name.
        /// </summary>
        public static DocumentedSwitch FindSwitch(string name)
        {
            foreach (var tuple in Documented)
            {
                foreach (var @switch in tuple.swtch)
                {
                    if (@switch.Names.Contains(name))
                    {
                        return @switch;
                    }
                }
            }


            throw new Exception($"Cannot find switch with name: {name}.");
        }

        public static List<(DocumentedSwitch, Dictionary<string, string>)>
            ParseSwitches(string[] args)
        {
            var result = new List<(DocumentedSwitch, Dictionary<string, string>)>();

            var currentArgs = new List<string>();
            DocumentedSwitch currentSwitch = null;
            foreach (var arg in args)
            {
                if (IsSwitch(arg))
                {
                    // Finish of the previous switch (if any)
                    if (currentSwitch != null)
                    {
                        result.Add((currentSwitch, currentSwitch.ParseExtraParams(currentArgs)));
                    }

                    currentArgs.Clear();
                    currentSwitch = FindSwitch(arg);
                }
                else
                {
                    currentArgs.Add(arg);
                }
            }
            if (currentSwitch != null)
            {
                result.Add((currentSwitch, currentSwitch.ParseExtraParams(currentArgs)));
            }
            return result;
        }
    }
}