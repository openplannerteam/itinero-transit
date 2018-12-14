using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journeys
{
    public static class JourneyExtensions
    {
        public static string ToGeoJson<T>(this Journey<T> journey, StopsDb stopsDb)
            where T : IJourneyStats<T>
        {
            var stringWriter = new StringWriter();
            journey.WriteGeoJson(stopsDb, stringWriter);
            return stringWriter.ToString();
        }




        
        
        
        
        
        
        
        /// <summary>
        /// Writes the route as json.
        /// </summary>
        public static void WriteGeoJson<T>(this Journey<T> journey, StopsDb stopsDb, TextWriter writer)
            where T : IJourneyStats<T>
        {
            if (journey == null)
            {
                throw new ArgumentNullException(nameof(journey));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var jsonWriter = new IO.Json.JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            var reader = stopsDb.GetReader();
            var originalJourney = journey;
            while (journey != null)
            {
                if (reader.MoveTo(journey.Location))
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Feature", true, false);
                    jsonWriter.WriteProperty("name", "Stop", true, false);
                    jsonWriter.WritePropertyName("geometry", false);

                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Point", true, false);
                    jsonWriter.WritePropertyName("coordinates", false);
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(
                        reader.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    jsonWriter.WriteArrayValue(
                        reader.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    jsonWriter.WriteArrayClose();
                    jsonWriter.WriteClose();

                    jsonWriter.WritePropertyName("properties");
                    jsonWriter.WriteOpen();

                    jsonWriter.WriteProperty("stop_uri", reader.GlobalId, true, true);

                    jsonWriter.WriteClose();

                    jsonWriter.WriteClose();
                }

                journey = journey.PreviousLink;
                
                while (journey != null && !journey.SpecialConnection)
                {
                    journey = journey.PreviousLink;
                }
            }

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WriteProperty("name", "ShapeMeta", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();

            journey = originalJourney;
            while (journey != null)
            {
                if (reader.MoveTo(journey.Location))
                {
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(reader.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    jsonWriter.WriteArrayValue(reader.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    jsonWriter.WriteArrayClose();
                }

                journey = journey.PreviousLink;
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();

            jsonWriter.WriteClose();

            jsonWriter.WriteClose();

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();
        }
    }
}