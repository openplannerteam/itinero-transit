namespace Itinero.Transit.IO.OSM.Writer
{
    internal static class OsmDataHandler
    {
        public static string ToOsmRouteType(string gtfsRouteType)
        {
            if (string.IsNullOrWhiteSpace(gtfsRouteType)) return string.Empty;
            
            // based on:
            // GTFS: https://github.com/itinero/GTFS/blob/develop/src/GTFS/Entities/Enumerations/RouteType.cs
            // OSM:  https://wiki.openstreetmap.org/wiki/Relation:route#Public_transport_routes

            switch (gtfsRouteType)
            {
                case "rail":
                    return "train";
                case "bus":
                    return "bus";
                case "tram":
                    return "tram";
                case "ferry":
                    return "ferry";
                case "subwaymetro":
                    return "subway";
                case "cablecar":
                case "gondola": 
                case "furnicular":
                    return "cablecar"; // TODO: https://wiki.openstreetmap.org/wiki/Tag:aerialway%3Dcable_car
            }

            return string.Empty;
        }
    }
}