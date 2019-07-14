using System;
using System.Collections.Generic;
using System.IO;
using IDP.Switches;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Utils;

namespace Itinero.Transit.DataProcessor.Transit
{
    class SwitchDumpTransitDbConnections : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--dump-connections"};

        private const string _about = "Writes all connections contained in a transitDB to console";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>()
                {
                    SwitchesExtensions.opt("file", "The file to write the data to, in .csv format")
                        .SetDefault("")
                };

        private const bool _isStable = true;


        public SwitchDumpTransitDbConnections
            () :
            base(_names, _about, _extraParams, _isStable)
        {
        }


        public void Use(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var writeTo = arguments["file"];

            using (var outStream =
                string.IsNullOrEmpty(writeTo) ? Console.Out : new StreamWriter(File.OpenWrite(writeTo)))
            {
                const string header = "GlobalId,DepartureStop,DepartureStopName,ArrivalStop,ArrivalStopName," +
                                      "DepartureTime,DepartureDelay,ArrivalTime,ArrivalDelay,TravelTime,Mode,TripId,TripHeadSign";
                outStream.WriteLine(header);


                var consDb = tdb.Latest.ConnectionsDb.GetReader();
                var dep = tdb.Latest.StopsDb.GetReader();
                var arr = tdb.Latest.StopsDb.GetReader();
                var tripsDb = tdb.Latest.TripsDb;

                var indexN = consDb.First();
                if (indexN == null)
                {
                    throw new ArgumentException("Cannnot dump an empty transitDb");
                }

                var index = indexN.Value;

                do
                {
                    var cons = consDb.Get(index);
                    
                    dep.MoveTo(cons.DepartureStop);
                    arr.MoveTo(cons.ArrivalStop);
                    var trip = tripsDb.Get(cons.TripId);

                    var value = $"{cons.GlobalId}," +
                                $"{dep.GlobalId}," +
                                $"{dep.Attributes.Get("name")}," +
                                $"{arr.GlobalId}," +
                                $"{arr.Attributes.Get("name")}," +
                                $"{cons.DepartureTime.FromUnixTime():O}," +
                                $"{cons.DepartureDelay}," +
                                $"{cons.ArrivalTime.FromUnixTime():O}," +
                                $"{cons.ArrivalDelay}," +
                                $"{(cons.ArrivalTime.FromUnixTime() - cons.DepartureTime.FromUnixTime()).TotalSeconds}," +
                                $"{cons.Mode}," +
                                $"{trip.GlobalId}," +
                                $"{trip.Attributes.Get("headsign")}";

                    outStream.WriteLine(value);

                } while (consDb.HasNext(index, out index));
            }
        }
    }    internal static class Helpers
    {
        public static string Get(this IAttributeCollection attributes, string name)
        {
            attributes.TryGetValue(name, out var result);
            return result;
        }
    }
}