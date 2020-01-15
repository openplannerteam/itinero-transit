using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Processor.Switch.Filter;
using Itinero.Transit.Processor.Switch.Read;
using Itinero.Transit.Processor.Switch.Validation;
using Itinero.Transit.Processor.Switch.Write;

namespace Itinero.Transit.Processor
{
    internal class HelpSwitch : DocumentedSwitch, ITransitDbModifier, ITransitDbSource, ITransitDbSink
    {
        private static readonly string[] _names = {"--help", "--?", "--h"};

        private static readonly List<(List<string>argName, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string>argName, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("about", "The command (or switch) you'd like more info about"),
                    SwitchesExtensions.opt("markdown", "md",
                        "Write the help text as markdown to a file. The documentation is generated with this flag."),
                    SwitchesExtensions.opt("experimental", "Include experimental switches in the output")
                        .SetDefault("false"),
                    SwitchesExtensions.opt("short", "Only print a small overview").SetDefault("false")
                };

        private const bool IsStable = true;
        private const string About = "Print the help message";

        public HelpSwitch() :
            base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDb Modify(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            PrintHelp(parameters);
            return transitDb;
        }

        public TransitDb Generate(Dictionary<string, string> parameters)
        {
            PrintHelp(parameters);
            return new TransitDb(0);
        }

        public void Use(Dictionary<string, string> parameters, TransitDbSnapShot _)
        {
            PrintHelp(parameters);
        }

        private static void PrintHelp(Dictionary<string, string> arguments)
        {
            if (!string.IsNullOrEmpty(arguments["about"]))
            {
                var needed = arguments["about"].ToLower();
                var origNeeded = needed;
                if (!needed.StartsWith("--"))
                {
                    needed = "--" + needed;
                }

                var allSwitches = SwitchParsers.Documented;
                var options = new List<string>();
                foreach (var (_, switches) in allSwitches)
                {
                    foreach (var documentedSwitch in switches)
                    {
                        foreach (var name in documentedSwitch.Names)
                        {
                            if (needed.Equals(name))
                            {
                                Console.WriteLine(documentedSwitch.Help());
                                return;
                            }
                        }

                        if (documentedSwitch.OptionNames.Contains(origNeeded))
                        {
                            options.Add(documentedSwitch.Names[0]);
                        }
                    }
                }

                var optionsFound = "";
                if (options.Any())
                {
                    optionsFound = $" However, a parameter with this name exist for {string.Join(", ", options)}";
                }

                throw new ArgumentException(
                    $"Did not find documentation for switch {needed}.{optionsFound}");
            }

            var shortVersion = arguments.Bool("short");
            var experimental = arguments.Bool("experimental");

            if (string.IsNullOrEmpty(arguments["markdown"]))
            {
                Console.Write(GenerateAllHelp(includeExperimental: experimental, shortVersion: shortVersion));
            }
            else
            {
                File.WriteAllText(arguments["markdown"],
                    GenerateAllHelp(true, experimental, shortVersion));
            }
        }


        private static string GenerateAllHelp(bool markdown = false, bool includeExperimental = false,
            bool shortVersion = false)
        {
            var text = "";

            if (shortVersion)
            {
                includeExperimental = true;
            }

            if (!shortVersion)
            {
                text += "Itinero Transit Processor \n";
                text += "========================= \n\n";
                text +=
                    "The **Itinero Transit Processor** *(ITP)* helps to convert various public transport datasets into a transitdb" +
                    " which can be used to quickly solve routing queries.\n\n";
            }


            if (!shortVersion)
            {
                text += string.Join("\n", new[]
                {
                    "",
                    "Usage",
                    "-----",
                    "",
                    "The switches act as 'mini-programs' which are executed one after another.",
                    "A switch will either create, modify or write this data. This document details what switches are available.",
                    "",
                    "In normal circumstances, only a single transit-db is loaded into memory." ,//However, ITP supports h",
                    "",
                    "Examples",
                    "--------",
                    "",
                    "A few useful examples to get you started:",
                    "",
                    "````" ,
                    $"itp {new ReadGTFS().Names[0]} gtfs.zip # read a gtfs archive",
                    $"        {new WriteTransitDb().Names[0]} # write the data into a transitdb, so that we can routeplan with it",
                    $"        {new WriteVectorTiles().Names[0]} # And while we are at it, generate vector tiles from them as well",
                    "````",
                    "",
                    "",
                    "````" ,
                    $"itp {new ReadTransitDb().Names[0]} data.transitdp # read a transitdb",
                    $"        {new WriteStops()} stops.csv # Create a stops.csv of all the stop locations and information",
                    $"        {new Validate().Names[0]} # Afterwards, check the transitdb for issues",
                    "````",
                    "",
                    "````" ,
                    $"itp {new ReadTransitDb().Names[0]} data.transitdp # read a transitdb",
                    $"        {new SelectTimeWindow().Names[0]} 2020-01-20T10:00:00 1hour # Select only connections departing between 10:00 and 11:00",
                    $"        {new SelectStopById()} http://some-agency.com/stop/123456 # Filter for this stop, and retain only connections and trips only using this stop",
                    $"        {new WriteConnections().Names[0]} # Write all the connections to console to inspect them",
                    "````",
                    "",
                    "````" ,
                    $"itp {new Shell().Names[0]} # Open an interactive shell, in order to experiment with the data",
                    "````",
                    "",

                    "Switch Syntax",
                    "-------------",
                    "",
                    "The syntax of a switch is:",
                    "",
                    "    --switch param1=value1 param2=value2",
                    "    # Or equivalent:",
                    "    --switch value1 value2",
                    "",
                    "There is no need to explicitly give the parameter name, ",
                    "as long as *unnamed* parameters are in the same order as in the tables below. ",
                    "It doesn't mater if only some arguments, all arguments or even no arguments are named: ",
                    "`--switch value2 param1=value1`, `--switch value1 param2=value2` or `--switch param1=value1 value2` ",
                    "are valid just as well.",
                    "",
                    "At last, `-param1` is a shorthand for `param=true`. This is useful for boolean flags.",
                    ""
                });
            }


            List<(string category, List<DocumentedSwitch>)> allSwitches;
            if (includeExperimental)
            {
                allSwitches = SwitchParsers.Documented;
            }
            else
            {
                // Only keep non-experimental switches
                allSwitches = new List<(string category, List<DocumentedSwitch>)>();
                foreach (var (cat, switches) in SwitchParsers.Documented)
                {
                    var sw = new List<DocumentedSwitch>();
                    foreach (var @switch in switches)
                    {
                        if (@switch.SwitchIsStable)
                        {
                            sw.Add(@switch);
                        }
                    }

                    if (sw.Any())
                    {
                        allSwitches.Add((cat, sw));
                    }
                }
            }


            // Build table of contents
            if (markdown)
            {
                text += "\n\nFull overview of all options ";
                text += "\n------------------------------- \n\n";
                if (!shortVersion)
                {
                    text +=
                        "All switches are listed below. Click on a switch to get a full overview, including sub-arguments.\n\n";
                }

                foreach (var (cat, switches) in allSwitches)
                {
                    text += $"- [{cat}](#{cat.Replace(" ", "-")})\n";
                    foreach (var @switch in switches)
                    {
                        text += $"  * [{@switch.Names[0]}](#";
                        text += @switch.MarkdownName().Replace(" ", "-").Replace(",", "").Replace("(", "")
                            .Replace(")", "");

                        text += ") ";
                        var about = @switch.Documentation;
                        var index = about.IndexOf('.');
                        text += index < 0 ? about : about.Substring(0, index + 1);
                        text += "\n";
                    }
                }
            }
            else
            {
                text += "Overview\n" +
                        "========\n\n";
                foreach (var (cat, switches) in allSwitches)
                {
                    text += "\n" + cat + "\n";


                    foreach (var @switch in switches)
                    {
                        text += $"   {@switch.Names[0]}\t";

                        var about = @switch.Documentation;
                        var index = about.IndexOf('.');
                        text += index < 0 ? about : about.Substring(0, index + 1);
                        text += "\n";
                    }
                }
            }

            if (shortVersion)
            {
                return text;
            }

            // Add docs
            foreach (var (cat, switches) in allSwitches)
            {
                text += $"### {cat}\n\n";

                foreach (var @switch in switches)
                {
                    text += @switch.Help(markdown) + "\n";
                }
            }

            return text;
        }
    }
}