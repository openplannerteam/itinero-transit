using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    internal class Shell : DocumentedSwitch, IMultiTransitDbSource, IMultiTransitDbModifier, IMultiTransitDbSink
    {
        private static readonly string[] _names = {"--shell", "--interactive", "--i"};

        private static string About =
            "Starts an interactive shell where switches can be used as commands";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = true;

        public Shell() : base(_names, About, _extraParams, IsStable)
        {
        }

        private static readonly string[] _units =
        {
            "bytes", "kb", "mb", "gb", "tb"
        };

        private static string FormatMemory(long byteCount)
        {
            var index = 0;
            var rest = 0;
            while (byteCount > 1000)
            {
                index++;
                rest = (int) (byteCount % 1000);
                byteCount /= 1000;
            }

            return $"{byteCount}.{rest:000}{_units[index]}";
        }

        private string Stats(DateTime lastActionStart)
        {
            using (var proc = Process.GetCurrentProcess())
            {
                var end = DateTime.Now;
                var timeNeeded = (end - lastActionStart).TotalSeconds;

                var ram = proc.WorkingSet64;
                var available = proc.VirtualMemorySize64;


                return
                    $"Last action took {timeNeeded:000}, memory is {FormatMemory(ram)}/{FormatMemory(available)}";
            }
        }

        private string StateMsg(TransitDb transitDb)
        {
            var tdb = transitDb.Latest;

            if (tdb == null)
            {
                return $"No transitdb loaded.";
            }

            if (tdb.ConnectionsDb.EarliestDate == ulong.MaxValue)
            {
                return $"Transitdb {tdb.GlobalId} is empty";
            }

            return
                $"Transitdb {tdb.GlobalId} contains {tdb.ConnectionsDb.Count()} connections between {tdb.ConnectionsDb.EarliestDate.FromUnixTime():s} and {tdb.ConnectionsDb.LatestDate.FromUnixTime():s}, " +
                $"{tdb.StopsDb.Count()} stops,  {tdb.TripsDb.Count()} trips.";
        }


        private IEnumerable<TransitDb> RunShell(IEnumerable<TransitDb> transitDbs)
        {
            using (var inStr = Console.In)
            {
                var start = DateTime.Now;

                while (true)
                {
                    try
                    {
                        Console.WriteLine("\nLoaded " + transitDbs.Count() + " transitdbs");
                        Console.WriteLine(string.Join("\n", transitDbs.Select(StateMsg)));

                        Console.WriteLine(Stats(start));
                        start = DateTime.Now;

                        Console.Write("--");
                        var line = inStr.ReadLine();
                        if (line == null || line.Equals("q"))
                        {
                            var r = new Random();
                            var i = r.Next(_endings.Length);
                            Console.WriteLine($"Quitting IDP-shell. {_endings[i]}");
                            break;
                        }


                        if (line.Equals(""))
                        {
                            continue;
                        }

                        line = "--" + line.Trim().TrimStart('-');


                        var switches = SwitchParsers.ParseSwitches(line.Split(" "));
                        if (switches.Count > 1)
                        {
                            Console.WriteLine("Multiple switches found, this is not supported");
                        }


                        transitDbs = transitDbs.ApplySwitch(switches[0]);
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return transitDbs;
        }

        public IEnumerable<TransitDb> Generate(Dictionary<string, string> parameters)
        {
            return RunShell(new List<TransitDb>());
        }

        public IEnumerable<TransitDb> Modify(Dictionary<string, string> parameters, List<TransitDb> transitDbs)
        {
            return RunShell(transitDbs);
        }


        /// <summary>
        /// Opgelet: Bevat humor van de bovenste plank
        /// </summary>
        private readonly string[] _endings =
        {
            "Have a pleasant day",
            "See you next time!",
            "Over and out.",
            "Computers follow your orders, not your intentions.",
            "How did the locomotive get so good at it’s job? Training",
            "How do you find a missing train? Follow the tracks",
            "What happened to the man that took the train home? He had to give it back!",
            "Why was the train late? It kept getting side tracked.",
            "In de mobiliteitsector, daar beweegt wat!",
            "Do your buses run on time? No, they run on diesel.",
            "Hoe kan je zien dat er recent een trein is gepasseerd? Omdat de sporen er nog zijn!",
            "A bus is a vehicle that runs twice as fast when you are after it as when you are in it.",
            "What did bus say to other bus? 'Beeep'",
            "Why was the bus 🚌sleeping? Because it was too tired",
            "The new Director of Public Transportation is obsessed with 'green' fuels. He's made all the buses run on thyme."
        };

        public void Use(Dictionary<string, string> parameters, IEnumerable<TransitDbSnapShot> transitDbs)
        {
            // This is a bit dirty
            var tdbs = transitDbs.Select(snapshot =>
                TransitDb.CreateMergedTransitDb(new List<TransitDbSnapShot> {snapshot}));
            RunShell(tdbs);
        }
    }
}