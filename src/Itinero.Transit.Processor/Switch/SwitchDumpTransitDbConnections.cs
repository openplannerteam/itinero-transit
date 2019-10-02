using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
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
                        .SetDefault(""),
                    SwitchesExtensions.opt("human", "Use less exact but more human-friendly output")
                        .SetDefault("false")
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
            var humanFormat = bool.Parse(arguments["human"]);

            using (var outStream =
                string.IsNullOrEmpty(writeTo) ? Console.Out : new StreamWriter(File.OpenWrite(writeTo)))
            {
                const string header = "GlobalId,DepartureStop,DepartureStopName,DepartureTime,DepartureDelay,ArrivalStop,ArrivalStopName," +
                                      "ArrivalTime,ArrivalDelay,TravelTime,Mode,TripId,TripHeadSign";
                const string headerHuman = 
                    "GlobalId,DepartureStopName,DepartureTime,DepartureDelay,ArrivalStopName," +
                                      "ArrivalTime,ArrivalDelay,TravelTime,Mode,TripId,TripHeadSign";

                
                
                outStream.WriteLine(humanFormat ? headerHuman : header);


                var consDb = tdb.Latest.ConnectionsDb;
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
                                $"{cons.DepartureTime.FromUnixTime():O}," +
                                $"{cons.DepartureDelay}," +
                                $"{arr.GlobalId}," +
                                $"{arr.Attributes.Get("name")}," +
                                $"{cons.ArrivalTime.FromUnixTime():O}," +
                                $"{cons.ArrivalDelay}," +
                                $"{(cons.ArrivalTime.FromUnixTime() - cons.DepartureTime.FromUnixTime()).TotalSeconds}," +
                                $"{cons.Mode}," +
                                $"{trip.GlobalId}," +
                                $"{trip.Attributes.Get("headsign")}";
                    
                    var valueHuman =
                                $"{cons.GlobalId}," +
                                $"{dep.Attributes.Get("name")}," +
                                $"{cons.DepartureTime.FromUnixTime():hh:mm}," +
                                $"{cons.DepartureDelay}," +
                                $"{arr.Attributes.Get("name")}," +
                                $"{cons.ArrivalTime.FromUnixTime():hh:mm}," +
                                $"{cons.ArrivalDelay}," +
                                $"{(cons.ArrivalTime.FromUnixTime() - cons.DepartureTime.FromUnixTime()).TotalSeconds}," +
                                $"{cons.Mode}," +
                                $"{trip.GlobalId}," +
                                $"{trip.Attributes.Get("headsign")}";

                    outStream.WriteLine(humanFormat ? valueHuman : value);

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