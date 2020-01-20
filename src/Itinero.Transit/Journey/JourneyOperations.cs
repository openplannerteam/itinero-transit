using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
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
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        public string ToString(IStopsDb stops)
        {

            string locName(StopId sId)
            {
                if (stops == null)
                {
                    return sId.ToString();
                }

                var s = stops.Get(sId);
                var nm = s?.GetName();
                if (!string.IsNullOrEmpty(""))
                {
                    return nm;
                }

                return s?.GlobalId ?? s.ToString();
            }

            var location = locName(Location);

            var texts = new List<string>
            {
                $"Arrive at {location} at {Time.FromUnixTime():s}"
            };

            var c = PreviousLink;

            while (c != null)
            {
             
                var msg = c.SpecialConnection
                    ? $"Walk/cycle to {location}, eta {c.Time.FromUnixTime():HH:mm}"
                    : $"    {c.Time.FromUnixTime():HH:mm} {locName(c.Location)} {c.Connection}";

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