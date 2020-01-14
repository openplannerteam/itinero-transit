using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchDumpTransitDbTrips : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--dump-trips"};

        private static string About = "Writes all trips contained in a transitDB to console or file";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("file", "The file to write the data to, in .csv format")
                        .SetDefault("")
                };

        private const bool IsStable = true;


        public SwitchDumpTransitDbTrips
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }

        public void Use(Dictionary<string, string> arguments, TransitDbSnapShot tdb)
        {
            var writeTo = arguments["file"];


            var trips = tdb.TripsDb;


            using (var outStream =
                string.IsNullOrEmpty(writeTo) ? Console.Out : new StreamWriter(File.OpenWrite(writeTo)))
            {
                var knownAttributes = new List<string>();

                foreach (var trip in trips)
                {
                    var attributes = trip.Attributes;
                    if (attributes == null) continue;
                    foreach (var (key, _) in attributes)
                    {
                        if (!knownAttributes.Contains(key))
                        {
                            knownAttributes.Add(key);
                        }
                    }
                }


                var header = "GlobalId";
                foreach (var knownAttribute in knownAttributes)
                {
                    header += "," + knownAttribute;
                }

                outStream.WriteLine(header);

                foreach (var trip in trips)
                {
                    var value =
                        $"{trip.GlobalId}";

                    var attributes = trip.Attributes;
                    if (attributes != null)
                    {
                        foreach (var attribute in knownAttributes)
                        {
                            attributes.TryGetValue(attribute, out var val);
                            value += $",{val ?? ""}";
                        }
                    }

                    outStream.WriteLine(value);
                }
            }
        }
    }
}