using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.IO.VectorTiles.Data
{
    internal class Route
    {
        public string ShortName { get; set; }
        
        public string LongName { get; set; }
        
        public string Color { get; set; }
        
        public string RouteType { get; set; }
        
        public string OperatorGlobalId { get; set; }

        public string ToJson()
        {
            return $"{{ \"shortname\": \"{this.ShortName}\"" +
                   $", \"operator_id\": \"{this.OperatorGlobalId}\"" +
                   $", \"route_type\": \"{this.RouteType}\"" +
                   $", \"longname\": \"{this.LongName}\"" +
                   $", \"color\": \"{this.Color}\"}}";
        }

        public static Route FromTrip(Trip trip, Func<OperatorId, Operator> getOperator = null)
        {
            var route = new Route();
            if (trip.TryGetAttribute("route_shortname", out var val))
            {
                route.ShortName = val;
            }

            if (trip.TryGetAttribute("route_longname", out val))
            {
                route.LongName = val;
            }

            if (trip.TryGetAttribute("route_type", out val))
            {
                route.RouteType = val;
            }

            if (trip.TryGetAttribute("route_color", out val))
            {
                route.Color = val;
            }

            if (getOperator != null)
            {
                var op = getOperator(trip.Operator);
                route.OperatorGlobalId = op.GlobalId;
            }

            return route;
        }
    }
}