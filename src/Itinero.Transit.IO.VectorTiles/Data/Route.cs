using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.IO.VectorTiles.Data
{
    internal class Route
    {
        public string ShortName { get; set; }
        
        public string LongName { get; set; }
        
        public string Color { get; set; }

        public string ToJson()
        {
            return $"{{ \"shortname\": \"{this.ShortName}\"" +
                   $", \"longname\": \"{this.LongName}\"" +
                   $", \"color\": \"{this.Color}\"}}";
        }

        public static Route FromTrip(Trip trip)
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

            if (trip.TryGetAttribute("route_color", out val))
            {
                route.Color = val;
            }

            return route;
        }
    }
}