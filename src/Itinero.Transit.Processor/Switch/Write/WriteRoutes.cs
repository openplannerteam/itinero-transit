using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor.Switch.Write
{
    class WriteRoutes : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--write-routes","--routes"};

        private static string About =
            "Create an overview of routes and shows them. A route is a list of stops, where at least one trip does all of them in order";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;


        public WriteRoutes
            () : base(_names, About, _extraParams, IsStable)
        {
        }

        private string TripData(Trip t)
        {
            var msg = "- ";
            if (t.TryGetAttribute("headsign", out var headsign))
            {
                msg += $"{headsign} ({t.GlobalId})";
            }
            else
            {
                msg += t.GlobalId;
            }

            var attributes = t.Attributes?.Select(kv => kv.Key + "=" + kv.Value);

            if (attributes != null)
            {
                msg += " ";
                msg += string.Join(", ",attributes);
            }

            return msg;
        }

        public void Use(Dictionary<string, string> parameters, TransitDbSnapShot transitDb)
        {
            var routeMerger = new RouteMerger();

            var connections = transitDb.Connections;
            var stops = transitDb.Stops;
            var trips = transitDb.Trips;

            foreach (var connection in connections)
            {
                routeMerger.AddConnection(connection);
            }

            var route2Trips = routeMerger.GetRouteToTrips();

            foreach (var kv in route2Trips)
            {
                var route = kv.Key;
                var allTrips = trips.GetAll(kv.Value);

                var routeStops = stops.GetAll(route.ToList());

                var stopStrings =
                    routeStops.Select(s => $" - {s.GetName()} ({s.GlobalId})");


                Console.WriteLine(
                    "\n Route \n=======\n\n" +
                    $"Stops in {routeStops.Count} stops:\n" +
                    string.Join("\n", stopStrings) +
                    $"\n{allTrips.Count} trips on this route:\n" +
                    string.Join("\n",
                        allTrips.Select(TripData)) +
                    "\n\n"
                );
            }
        }
    }
}