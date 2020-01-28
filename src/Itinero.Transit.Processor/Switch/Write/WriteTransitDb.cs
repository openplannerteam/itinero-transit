// The MIT License (MIT)


using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch.Write
{
    /// <summary>
    /// Represents a switch to read a shapefile for routing.
    /// </summary>
    class WriteTransitDb : DocumentedSwitch, IMultiTransitDbSink
    {
        private static readonly string[] _names =
            {"--write-transit-db", "--write-transitdb", "--write-transit", "--write", "--wt"};

        private static readonly string _about = "Write a transitDB to disk";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("file", "The output file to write to")
                        .SetDefault("$operatorName.YYYY-mm-dd.transitdb"),
                };

        private const bool IsStable = true;


        public WriteTransitDb()
            : base(_names, _about, _extraParams, IsStable)
        {
        }


        public void Use(Dictionary<string, string> arguments, List<TransitDbSnapShot> tdbs)
        {
            foreach (var tdb in tdbs)
            {
                var fileName = arguments["file"];

                if (fileName.Equals("$operatorName.YYYY-mm-dd.transitdb"))
                {
                    fileName =
                        $"{tdb.GetAttribute("name", tdb.GlobalId)}.{tdb.EarliestDate().Date:yyyy-MM-dd}.transitdb";
                }

                fileName = fileName.Replace("/", "_")
                    .Replace(" ", "_")
                    .Replace(",","_")
                    .Replace("%","_");

                using (var stream = File.OpenWrite(fileName))
                {
                    tdb.WriteTo(stream);
                    Console.WriteLine(
                        $"Written {fileName}, transitDb is valid from {tdb.Connections.EarliestDate.FromUnixTime():s} till {tdb.Connections.LatestDate.FromUnixTime():s} ");
                }
            }
        }
    }
}