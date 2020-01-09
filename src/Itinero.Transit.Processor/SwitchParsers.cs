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
                    new SwitchCreateTransitDbOSM(),
                    new SwitchCreateTransitDbGTFS()
                }),

                ("Filtering the transitdb", new List<DocumentedSwitch>
                {
                    new SwitchSelectTimeWindow(),
                    new SwitchSelectStopsByBoundingBox(),
                    new SwitchSelectStopById(),
                    new SwitchSelectTrip(),
                    new SwitchUnusedFilter()
                }),

                ("Saving to and from file", new List<DocumentedSwitch>
                {
                    new SwitchReadTransitDb(),
                    new SwitchDumpTransitDbStops(),
                    new SwitchDumpTransitDbConnections(),
                    new SwitchDumpTransitDbTrips(),
                    new SwitchWriteTransitDb(),
                }),

                ("Misc", new List<DocumentedSwitch>

                {
                    new SwitchJapanize(),
                    new SwitchValidate(),
                    new HelpSwitch(),
                    new Shell(),
                    new SwitchClear(),
                    new SwitchGc()
                })
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


            throw new ArgumentException($"Cannot find switch with name: {name}.");
        }

        public static List<(DocumentedSwitch, Dictionary<string, string>)>
            ParseSwitches(string[] args)
        {
            CheckNoDuplicateNames();
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

        private static void CheckNoDuplicateNames()
        {
            var knownNames = new HashSet<string>();
            foreach (var (_, swtchs) in Documented)
            {
                foreach (var swtch in swtchs)
                {
                    foreach (var name in swtch.Names)
                    {
                        if (!knownNames.Contains(name))
                        {
                            knownNames.Add(name);
                            continue;
                        }

                        throw new ArgumentException(
                            $"Bug in Processor: Name {name} already exists. If you can read this, something went very wrong.");
                    }
                }
            }
        }
    }
}