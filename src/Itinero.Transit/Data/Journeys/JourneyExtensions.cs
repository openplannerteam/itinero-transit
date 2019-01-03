using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journeys
{
    // ReSharper disable once UnusedMember.Global
    public static class JourneyExtensions
    {
        // ReSharper disable once UnusedMember.Global
        public static string ToGeoJson<T>(this Journey<T> journey, StopsDb stopsDb)
            where T : IJourneyStats<T>
        {
            var stringWriter = new StringWriter();
            journey.WriteGeoJson(stopsDb, stringWriter);
            return stringWriter.ToString();
        }


        public static List<Journey<T>> AllParts<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var parts = new List<Journey<T>>();
            var current = j;
            do
            {
                parts.Add(current);
                current = current.PreviousLink;
            } while (current != null && !ReferenceEquals(current, current.PreviousLink));

            return parts;
        }

        public static List<Journey<T>> Reversed<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var l = new List<Journey<T>>();
            Reversed(j, l);
            return l;
        }

        /// <summary>
        /// Reverses and flattens the journey.
        /// The resulting, new journeys will not contain alternative choices and will be added to the list
        /// </summary>
        /// <returns></returns>
        public static void Reversed<T>(this Journey<T> j, List<Journey<T>> addTo) where T : IJourneyStats<T>
        {
            Reversed(j, new Journey<T>(j.Location, j.Time, j.Stats.EmptyStat(), j.Root.TripId), addTo);
        }

        private static void Reversed<T>(this Journey<T> j, Journey<T> buildOn, List<Journey<T>> addTo)
            where T : IJourneyStats<T>
        {
            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                // We have arrived at the end of the journey, all information should be added already
                addTo.Add(buildOn);
                return;
            }

            if (j.SpecialConnection && j.Connection == Journey<T>.JOINED_JOURNEYS)
            {
                j.PreviousLink.Reversed(buildOn, addTo);
                j.AlternativePreviousLink.Reversed(buildOn, addTo);
                return;
            }

            if (j.SpecialConnection)
            {
                buildOn = buildOn.ChainSpecial(
                    j.Connection, j.PreviousLink.Time, j.PreviousLink.Location,
                    j.TripId);
            }
            else
            {
                buildOn = buildOn.Chain(
                    j.Connection, j.PreviousLink.Time, j.PreviousLink.Location,
                    j.TripId);
            }


            j.PreviousLink.Reversed(buildOn, addTo);
        }


        public static Journey<T> Pruned<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var restOfTheJourney = j.PreviousLink.PrunedWithoutLast();
            return restOfTheJourney.Chain(j.Connection, j.Time, j.Location, j.TripId);
        }

        /// <summary>
        /// Creates a new journey, where only important stops are retained. The intermediate stops are scrapped
        /// </summary>
        /// <returns></returns>
        private static Journey<T> PrunedWithoutLast<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                return j;
            }

            if (j.SpecialConnection)
            {
                var restOfTheJourney = j.PreviousLink.PrunedWithoutLast();
                return restOfTheJourney.ChainSpecial(
                    j.Connection, j.Time, j.Location, j.PreviousLink.TripId);
            }

            return j.PreviousLink.PrunedWithoutLast();
        }

        /// <summary>
        /// Writes the route as json.
        /// </summary>
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
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
                    jsonWriter.WriteArrayValue(
                        reader.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    jsonWriter.WriteArrayValue(
                        reader.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));

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