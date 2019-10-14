using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor
{
    internal class Shell : DocumentedSwitch, ITransitDbSource, ITransitDbSink, ITransitDbModifier
    {
        private static readonly string[] _names = {"--shell", "--interactive", "--i"};

        private static string _about =
            "Starts an interactive shell where switches can be used as commands";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool _isStable = true;

        public Shell() : base(_names, _about, _extraParams, _isStable)
        {
        }

        private string StateMsg(TransitDb tdb)
        {
            var snapshot = tdb.Latest;
            if (snapshot == null)
            {
                return "No transitdb loaded";
            }

            if (snapshot.ConnectionsDb.EarliestDate == ulong.MaxValue)
            {
                return "No transitdb loaded";
            }

            return
                $"Transitdb spans {snapshot.ConnectionsDb.EarliestDate.FromUnixTime():s} to {snapshot.ConnectionsDb.LatestDate.FromUnixTime():s}";
        }

        private TransitDb RunShell(TransitDb transitDb)
        {
            using (var inStr = Console.In)
            {
                while (true)
                {
                    Console.WriteLine("\n\n"+StateMsg(transitDb));
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

                    if (!line.StartsWith("--"))
                    {
                        line = "--" + line;
                    }

                    try
                    {
                        var sw = SwitchParsers.ParseSwitches(line.Split(" "));
                        foreach (var (swtch, parameters) in sw)
                        {
                            if (swtch is Shell)
                            {
                                Console.WriteLine(
                                    "Already in an interactive shell. Shell-in-shell is disabled, ignoring command");
                                continue;
                            }
                            
                            if (swtch is ITransitDbModifier modif)
                            {
                                transitDb = modif.Modify(parameters, transitDb);
                                continue;
                            }

                            if (swtch is ITransitDbSink sink)
                            {
                                sink.Use(parameters, transitDb);
                                continue;
                            }

                            if (swtch is ITransitDbSource src)
                            {
                               transitDb = src.Generate(parameters);
                               continue;
                            }

                            throw new ArgumentException("Unknown switch type: " + swtch.Names[0]);
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine(e.Message);
                    }catch (FormatException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return transitDb;
        }

        public TransitDb Generate(Dictionary<string, string> parameters)
        {
            return RunShell(new TransitDb(0));
        }

        public void Use(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            RunShell(transitDb);
        }

        public TransitDb Modify(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            return RunShell(transitDb);
        }

        private readonly string[] _endings = {
            "Have a pleasant day",
            "See you next time!",
            "Over and out.",
            "Computers follow your orders, not your intentions.",
            "How did the locomotive get so good at it’s job? Training",
            "How do you find a missing train? Follow the tracks",
            "What happened to the man that took the train home?He had to give it back!",
            "Why was the train late? It kept getting side tracked."
        };
    }
}