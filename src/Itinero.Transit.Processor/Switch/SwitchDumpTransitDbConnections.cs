using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchDumpTransitDbConnections : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--dump-connections"};

        private const string About = "Writes all connections contained in a transitDB to console";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>()
                {
                    SwitchesExtensions.opt("file", "The file to write the data to, in .csv format")
                        .SetDefault(""),
                    SwitchesExtensions.opt("human", "Use less exact but more human-friendly output")
                        .SetDefault("false")
                };

        private const bool IsStable = true;


        public SwitchDumpTransitDbConnections
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }


        public void Use(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var writeTo = arguments["file"];
            var humanFormat = bool.Parse(arguments["human"]);

            using (var outStream =
                string.IsNullOrEmpty(writeTo) ? Console.Out : new StreamWriter(File.OpenWrite(writeTo)))
            {
                const string header =
                    "GlobalId,DepartureStop,DepartureStopName,DepartureTime,DepartureDelay,ArrivalStop,ArrivalStopName," +
                    "ArrivalTime,ArrivalDelay,TravelTime,Mode,TripId,TripHeadSign";
                const string headerHuman =
                    "GlobalId,DepartureStopName,DepartureTime,DepartureDelay,ArrivalStopName," +
                    "ArrivalTime,ArrivalDelay,TravelTime,Mode,TripId,TripHeadSign";


                outStream.WriteLine(humanFormat ? headerHuman : header);


                var connections = tdb.Latest.ConnectionsDb;
                var stops = tdb.Latest.StopsDb;
                var trips = tdb.Latest.TripsDb;

                if (!connections.Any())
                {
                    throw new ArgumentException("Can not dump an empty transitDb");
                }

                foreach (var connection in connections)
                {
                    var dep = stops.Get(connection.DepartureStop);
                    var arr = stops.Get(connection.ArrivalStop);
                    var trip = trips.Get(connection.TripId);

                    var value = $"{connection.GlobalId}," +
                                $"{dep.GlobalId}," +
                                $"{dep.Attributes.Get("name")}," +
                                $"{connection.DepartureTime.FromUnixTime():O}," +
                                $"{connection.DepartureDelay}," +
                                $"{arr.GlobalId}," +
                                $"{arr.Attributes.Get("name")}," +
                                $"{connection.ArrivalTime.FromUnixTime():O}," +
                                $"{connection.ArrivalDelay}," +
                                $"{(connection.ArrivalTime.FromUnixTime() - connection.DepartureTime.FromUnixTime()).TotalSeconds}," +
                                $"{connection.Mode}," +
                                $"{trip.GlobalId}," +
                                $"{trip.Attributes.Get("headsign")}";

                    var valueHuman =
                        $"{connection.GlobalId}," +
                        $"{dep.Attributes.Get("name")}," +
                        $"{connection.DepartureTime.FromUnixTime():hh:mm}," +
                        $"{connection.DepartureDelay}," +
                        $"{arr.Attributes.Get("name")}," +
                        $"{connection.ArrivalTime.FromUnixTime():hh:mm}," +
                        $"{connection.ArrivalDelay}," +
                        $"{(connection.ArrivalTime.FromUnixTime() - connection.DepartureTime.FromUnixTime()).TotalSeconds}," +
                        $"{connection.Mode}," +
                        $"{trip.GlobalId}," +
                        $"{trip.Attributes.Get("headsign")}";

                    outStream.WriteLine(humanFormat ? valueHuman : value);
                }

            }
        }
    }

    internal static class Helpers
    {
        public static string Get(this Dictionary<string, string> attributes, string name)
        {
            attributes.TryGetValue(name, out var result);
            return result;
        }
    }
}