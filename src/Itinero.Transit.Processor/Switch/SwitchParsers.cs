using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Processor.Switch.Filter;
using Itinero.Transit.Processor.Switch.Misc;
using Itinero.Transit.Processor.Switch.Read;
using Itinero.Transit.Processor.Switch.Validation;
using Itinero.Transit.Processor.Switch.Write;

namespace Itinero.Transit.Processor.Switch
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
                ("Reading data", new List<DocumentedSwitch>
                {
                    new ReadLinkedConnections(),
                    new ReadOsmRelation(),
                    new ReadGTFS(),
                    new ReadTransitDb(),
                }),

                ("Filtering the transitdb", new List<DocumentedSwitch>
                {
                    new SelectTimeWindow(),
                    new SelectStopsByBoundingBox(),
                    new SelectStopById(),
                    new SelectTrip(),
                }),

                ("Validating and testing the transitdb", new List<DocumentedSwitch>
                {
                    new Validate(),
                    new RemoveDelays(),
                    new RemoveUnused(),
                    new ShowInfo(),
                }),

                ("Writing to file and to other formats", new List<DocumentedSwitch>
                {
                    new WriteTransitDb(),
                    new WriteVectorTiles(),
                    new WriteStops(),
                    new WriteConnections(),
                    new WriteRoutes(),
                    new WriteTrips(),
                }),

                ("Misc", new List<DocumentedSwitch>

                {
                    new HelpSwitch(),
                    new Shell(),
                    new Clear(),
                    new GarbageCollect()
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
            foreach (var argm in args)
            {

                if (argm.Length == 0)
                {
                    continue;
                }

                var arg = argm.Trim();
                if (argm.StartsWith("\"") && argm.EndsWith("\""))
                {
                    arg = argm.Substring(1, argm.Length - 2);
                }
                
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