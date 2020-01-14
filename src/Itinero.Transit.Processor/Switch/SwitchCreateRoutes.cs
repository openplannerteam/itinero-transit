using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchCreateRoutes : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--get-routes"};

        private static string About =
            "Create an overview of routes and shows them. A route is a list of stops, where at least one trip does all of them in order";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                };

        private const bool IsStable = false;


        public SwitchCreateRoutes
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

        public void Use(Dictionary<string, string> parameters, TransitDb transitDb)
        {
            var routeMerger = new RouteMerger();

            var connections = transitDb.Latest.ConnectionsDb;
            var stops = transitDb.Latest.StopsDb;
            var trips = transitDb.Latest.TripsDb;

            foreach (var connection in connections)
            {
                routeMerger.AddConnection(connection);
            }

            var route2trips = routeMerger.GetRouteToTrips();

            foreach (var kv in route2trips)
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