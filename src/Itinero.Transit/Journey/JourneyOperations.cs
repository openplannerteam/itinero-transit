using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;
using Reminiscence.Collections;

namespace Itinero.Transit.Journey
{
    public partial class Journey<T>
    {
        /// <summary>
        /// Given a journey and a reversed journey, append the reversed journey to the journey
        /// </summary>
        [Pure]
        public Journey<T> Append(Journey<T> restingJourney)
        {
            var j = this;
            while (restingJourney != null &&
                   (!restingJourney.SpecialConnection ||
                    !Equals(restingJourney.Connection, GENESIS)))
            {
                // Resting journey is backwards - so restingJourney is departure, restingJourney.PreviousLink the arrival time
                var timeDiff =
                    (long) restingJourney.Time -
                    (long) restingJourney.PreviousLink.Time; // Cast to long to allow negative values
                j = new Journey<T>(
                    j.Root,
                    j,
                    restingJourney.SpecialConnection,
                    restingJourney.Connection,
                    restingJourney.PreviousLink.Location,
                    j.Time + (ulong) timeDiff,
                    restingJourney.TripId,
                    j.Metric
                );
                restingJourney = restingJourney.PreviousLink;
            }

            return j;
        }

        /// <summary>
        /// Converts an entire journey into a neat overview
        /// </summary>
        /// <param name="maxLength"></param>
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        public string ToString(int maxLength, TransitDb dataProvider = null)
        {
            var stop = dataProvider?.Latest?.StopsDb?.Get(Location);
            var location =
                stop?.GetName() ?? stop?.GlobalId ?? Location.ToString();

            var texts = new List<string>
            {
                $"Arrive at {location} at {Time.FromUnixTime():s}"
            };

            var c = PreviousLink;

            while (c != null)
            {
                stop = dataProvider?.Latest?.StopsDb?.Get(c.Location);
                location =
                    stop?.GetName() ?? stop?.GlobalId ?? c.Location.ToString();


                var msg = SpecialConnection
                    ? $"Walk/cycle to {location}, eta {c.Time.FromUnixTime():HH:mm}"
                    : $"    {c.Time.FromUnixTime():HH:mm} {location} {c.Connection}";

                if (c.PreviousLink == null)
                {
                    texts.Add(msg);
                    msg = $"Depart from {location} at {c.Time.FromUnixTime():s}";
                }


                texts.Add(msg);
                c = c.PreviousLink;
            }

            return string.Join("\n", texts.Reverse());
        }
    }
}