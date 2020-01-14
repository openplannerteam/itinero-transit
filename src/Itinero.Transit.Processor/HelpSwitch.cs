using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;

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

        public void Use(Dictionary<string, string> parameters, TransitDb _)
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
                    GenerateAllHelp(true,  experimental, shortVersion));
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
                text +=
                    "\n\n" +
                    "Switch Syntax\n" +
                    "-------------\n\n" +
                    "The syntax of a switch is:\n\n" +
                    "    --switch param1=value1 param2=value2\n" +
                    "    # Or equivalent:\n" +
                    "    --switch value1 value2\n" +
                    "\n\nThere is no need to explicitly give the parameter name, as long as *unnamed* parameters" +
                    " are in the same order as in the tables below. " +
                    "It doesn't mater if only some arguments, all arguments or even no arguments are named. " +
                    "`--switch value2 param1=value1`, `--switch value1 param2=value2` or `--switch param1=value1 value2` " +
                    "are valid just as well.";
                text += "\n\n";
                text += "At last, `-param1` is a shorthand for `param=true`. This is useful for boolean flags\n\n";
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