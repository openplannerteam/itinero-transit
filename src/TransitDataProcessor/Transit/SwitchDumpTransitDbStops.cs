using System;
using System.Collections.Generic;
using System.IO;
using IDP.Switches;
using Itinero.Transit.Data;

namespace Itinero.Transit.DataProcessor.Transit
{
    class SwitchDumpTransitDbStops : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--dump-stops"};

        private static string _about = "Writes all stops contained in a transitDB to console";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("file", "The file to write the data to, in .csv format")
                        .SetDefault("")
                };

        private const bool _isStable = true;


        public SwitchDumpTransitDbStops
            () :
            base(_names, _about, _extraParams, _isStable)
        {
        }

        public void Use(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var writeTo = arguments["file"];


            var stops = tdb.Latest.StopsDb.GetReader();


            using (var outStream =
                string.IsNullOrEmpty(writeTo) ? Console.Out : new StreamWriter(File.OpenWrite(writeTo)))
            {
                var knownAttributes = new List<string>();
                while (stops.MoveNext())
                {
                    var attributes = stops.Attributes;
                    foreach (var attribute in attributes)
                    {
                        if (!knownAttributes.Contains(attribute.Key))
                        {
                            knownAttributes.Add(attribute.Key);
                        }
                    }
                }


                var header = "globalId,Latitude,Longitude,tileId_internalId";
                foreach (var knownAttribute in knownAttributes)
                {
                    header += "," + knownAttribute;
                }

                outStream.WriteLine(header);


                stops = tdb.Latest.StopsDb.GetReader();
                while (stops.MoveNext())
                {
                    var value =
                        $"{stops.GlobalId},{stops.Latitude}, {stops.Longitude},{stops.Id.LocalTileId}_{stops.Id.LocalId}";

                    var attributes = stops.Attributes;
                    foreach (var attribute in knownAttributes)
                    {
                        attributes.TryGetValue(attribute, out var val);
                        value += $",{val ?? ""}";
                    }

                    outStream.WriteLine(value);
                }
            }
        }
    }
}