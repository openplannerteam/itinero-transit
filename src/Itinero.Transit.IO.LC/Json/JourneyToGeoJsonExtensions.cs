using System;
using System.Globalization;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

// ReSharper disable UnusedMember.Global

namespace Itinero.Transit.IO.LC.Json
{
    public static class JourneyToGeoJsonExtensions
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
        // ReSharper disable once MemberCanBePrivate.Global
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

            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true);
            jsonWriter.WritePropertyName("features");
            jsonWriter.WriteArrayOpen();

            var reader = stopsDb.GetReader();
            var originalJourney = journey;
            while (journey != null)
            {
                if (reader.MoveTo(journey.Location))
                {
                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Feature", true);
                    jsonWriter.WriteProperty("name", "Stop", true);
                    jsonWriter.WritePropertyName("geometry");

                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Point", true);
                    jsonWriter.WritePropertyName("coordinates");
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(
                        reader.Longitude.ToString(CultureInfo.InvariantCulture));
                    jsonWriter.WriteArrayValue(
                        reader.Latitude.ToString(CultureInfo.InvariantCulture));

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
            jsonWriter.WriteProperty("type", "Feature", true);
            jsonWriter.WriteProperty("name", "ShapeMeta", true);
            jsonWriter.WritePropertyName("geometry");

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true);
            jsonWriter.WritePropertyName("coordinates");
            jsonWriter.WriteArrayOpen();

            journey = originalJourney;
            while (journey != null)
            {
                if (reader.MoveTo(journey.Location))
                {
                    jsonWriter.WriteArrayOpen();
                    jsonWriter.WriteArrayValue(
                        reader.Longitude.ToString(CultureInfo.InvariantCulture));
                    jsonWriter.WriteArrayValue(
                        reader.Latitude.ToString(CultureInfo.InvariantCulture));

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