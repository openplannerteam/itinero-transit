using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor
{
    internal class Shell : DocumentedSwitch, ITransitDbSource, ITransitDbSink
    {
        private static readonly string[] _names = {"--shell", "--interactive", "--i"};

        private static string _about =
            "Starts an interactive shell where switches can be used as commands";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool _isStable = false;

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

        private TransitDb RunShell()
        {
            var transitDb = new TransitDb(0);
            using (var inStr = Console.In)
            {
                while (true)
                {
                    Console.WriteLine("\n\n"+StateMsg(transitDb));
                    Console.Write("> ");
                    var line = inStr.ReadLine();
                    if (line == null || line.Equals("q"))
                    {
                        Console.WriteLine("Received end-of-file, quitting");
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
            return RunShell();
        }

        public void Use(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            RunShell();
        }
    }
}