using System;
using System.Diagnostics.CodeAnalysis;
using Itinero.Transit.Data.Core;

// ReSharper disable BuiltInTypeReferenceStyle

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.Journey.Metric
{
    using TimeSpan = UInt32;


    /// <inheritdoc />
    /// <summary>
    /// A simple metric keeping track of the number of trains taken and the total travel time.
    /// This class uses Pareto Optimization. Use either TotalTimeMinimizer or TotalTransferMinimizer to optimize for one of those
    /// </summary>
    public class TransferMetric : IJourneyMetric<TransferMetric>
    {
        public static readonly MinimizeAll ParetoCompare = new MinimizeAll();


        // ----------------- ZERO ELEMENT ------------------

        public static readonly TransferMetric Factory =
            new TransferMetric(0, 0, 0);


        // ---------------- ACTUAL METRICS -------------------------

        public readonly uint NumberOfVehiclesTaken;

        /// <summary>
        /// The total travel time, including 'in-vehicle'-time, walking time and waiting time
        /// </summary>
        public readonly TimeSpan TravelTime;

        /// <summary>
        /// The amount of time spent in 'other modes'
        /// </summary>
        public readonly float WalkingTime;

        private TransferMetric(uint numberOfVehiclesTaken,
            TimeSpan travelTime,
            float walkingWaitingTime)
        {
            NumberOfVehiclesTaken = numberOfVehiclesTaken;
            TravelTime = travelTime;
            WalkingTime = walkingWaitingTime;
        }

        public TransferMetric Zero()
        {
            return Factory;
        }

        public TransferMetric Add(Journey<TransferMetric> previousJourney, StopId currentLocation, ulong currentTime,
            TripId currentTripId,
            bool currentIsSpecial)
        {
            // We are in a new vehicle if...
            var newVehicle =
                // THis is _not_ a special journey
                !currentIsSpecial &&
                (
                    // the tripID of the previous journey is different from the current trip ID
                    !Equals(previousJourney.TripId, currentTripId)
                    // Or if the previous was a special connection. (Note that 
                    || previousJourney.SpecialConnection);


            ulong travelTime;

            if (currentTime > previousJourney.Time)
            {
                travelTime = currentTime - previousJourney.Time;
            }
            else
            {
                travelTime = previousJourney.Time - currentTime;
            }


            ulong walkingTime = 0;
            if (currentIsSpecial)
            {
                walkingTime = travelTime;
            }

            return new TransferMetric((uint) (NumberOfVehiclesTaken + (newVehicle ? 1 : 0)),
                (uint) (TravelTime + travelTime),
                WalkingTime + walkingTime);
        }

        public override string ToString()
        {
            var seconds = TravelTime == uint.MaxValue ? 0 : TravelTime;
            var hours = TravelTime / (60 * 60);
            seconds = seconds % (60 * 60);
            var minutes = seconds / 60;
            seconds = seconds % 60;

            return
                $"Metric: {NumberOfVehiclesTaken} vehicles taken, {hours}:{minutes}:{seconds} total time), {WalkingTime} seconds to walk";
        }

        private bool Equals(TransferMetric other)
        {
            return NumberOfVehiclesTaken == other.NumberOfVehiclesTaken
                   && TravelTime == other.TravelTime
                   && WalkingTime.Equals(other.WalkingTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TransferMetric) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) NumberOfVehiclesTaken;
                hashCode = (hashCode * 397) ^ (int) TravelTime;
                hashCode = (hashCode * 397) ^ WalkingTime.GetHashCode();
                return hashCode;
            }
        }
    }


    public class MinimizeAll : MetricComparator<TransferMetric>
    {
        [SuppressMessage("ReSharper", "RedundantIfElseBlock")]
        public override int ADominatesB(TransferMetric am, TransferMetric bm)
        {
            // Returns (-1) if A is smaller (and thus more optimized),
            // Return 1 if B is smaller (and thus more optimized)
            // Return 0 if they are equally optimal


            if (am.NumberOfVehiclesTaken == bm.NumberOfVehiclesTaken)
            {
                if (am.TravelTime < bm.TravelTime)
                {
                    return -1;
                }
                else if (am.TravelTime > bm.TravelTime)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else if (am.NumberOfVehiclesTaken < bm.NumberOfVehiclesTaken)
            {
                // a is clearly better on this dimension...
                // Is it also better on the other dimension?

                if (am.TravelTime <= bm.TravelTime)
                {
                    // A is better (or equally good) on the other aspect too
                    return -1;
                }
                else
                {
                    // B is better on the other dimension: no comparison possible
                    return int.MaxValue;
                }
            }
            else /* am.NumberOfTransfers > bm.NumberOfTransfers*/
            {
                // b is clearly better on this dimension...
                // Is it also better on the other dimension?

                if (am.TravelTime >= bm.TravelTime)
                {
                    // B is better (or equally good) on the other aspect too
                    return 1;
                }
                else
                {
                    // B is better on the other dimension: no comparison possible
                    return int.MaxValue;
                }
            }
        }

        public override int NumberOfDimension()
        {
            return 2;
        }
    }
}