using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;

namespace Itinero.Transit.Processor.Switch.Read
{
    /// <summary>
    /// Represents a switch to read a shapefile for routing.
    /// </summary>
    class ReadTransitDb : DocumentedSwitch, ITransitDbSource
    {
        private static readonly string[] _names = {"--read-transit-db", "--read-transit", "--read-tdb", "--rt", "--rtdb"};

        private static string About =
            "Read a transitDB file as input to do all the data processing. A transitDB is a database containing connections between multiple stops";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.obl("file", "The input file to read"),
                };

        private const bool IsStable = true;


        public ReadTransitDb()
            : base(_names, About, _extraParams, IsStable)
        {
        }


        public TransitDb Generate(Dictionary<string, string> arguments)
        {
            var fileName = arguments["file"];

            using (var stream = File.OpenRead(fileName))
            {
                var tdb = new TransitDb(0);
                var wr = tdb.GetWriter();
                wr.ReadFrom(stream);
                wr.Close();
                return tdb;
            }
        }
    }
}