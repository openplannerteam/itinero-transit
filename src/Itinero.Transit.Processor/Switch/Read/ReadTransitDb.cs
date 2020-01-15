using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;

namespace Itinero.Transit.Processor.Switch.Read
{
    /// <summary>
    /// Represents a switch to read a shapefile for routing.
    /// </summary>
    class ReadTransitDb : DocumentedSwitch, IMultiTransitDbSource
    {
        private static readonly string[] _names =
            {"--read-transit-db", "--read-transit", "--read-tdb", "--rt", "--rtdb", "--read"};

        private static string About =
            "Read a transitDB file as input to do all the data processing. A transitDB is a database containing connections between multiple stops";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("file", "The input file(s) to read, ',' seperated")
                        .SetDefault("*.transitdb"),
                };

        private const bool IsStable = true;


        public ReadTransitDb()
            : base(_names, About, _extraParams, IsStable)
        {
        }


        public IEnumerable<TransitDb> Generate(Dictionary<string, string> arguments)
        {
            var files = arguments.GetFilesMatching("file");

            return files.Select((file, i) =>
            {
                using (var stream = File.OpenRead(file))
                {
                    Console.WriteLine("Reading " + file);
                    var tdb = new TransitDb((uint) i);
                    var wr = tdb.GetWriter();
                    wr.ReadFrom(stream);
                    wr.Close();
                    return tdb;
                }
            }).ToList(); // ToList forces execution
        }
    }
}