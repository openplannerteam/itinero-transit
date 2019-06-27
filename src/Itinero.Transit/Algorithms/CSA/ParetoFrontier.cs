using System;
using System.Collections.Generic;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Algorithms.CSA
{
    public class ParetoFrontier<T>
        where T : IJourneyMetric<T>

    {
        public readonly MetricComparator<T> Comparator;
        public readonly IJourneyFilter<T> JourneyFilter;

        /// <summary>
        /// Contains all the points on the frontier, sorted by descending Journey.Time (thus the earliest departure is last in the list)
        /// This is needed for certain optimisations.
        /// Note that most removals (if they happen) will probably be on the tail, so not have too much of an performance impact
        /// </summary>
        public readonly List<Journey<T>> Frontier = new List<Journey<T>>();

        /// <summary>
        ///
        /// The ShadowIndex contains the departure times of a subset of Journeys from Frontier (but is equally long).
        /// ShadowIndex[i] contains the departure time of a journey which will
        /// - Depart earlier then (or at the same time as) frontier[i]
        /// - Which is located at frontier[j] (with j smaller then i)
        ///
        /// Often, ShadowIndex[i] will be equals to Frontier[i].Root.Time, but this is not always the case 
        /// 
        /// </summary>
        public readonly List<ulong> ShadowIndex = new List<ulong>();


        public ParetoFrontier(MetricComparator<T> comparator, IJourneyFilter<T> journeyFilter)
        {
            Comparator = comparator ?? throw new ArgumentNullException(nameof(comparator),
                             "A Pareto Frontier can not operate without comparator");
            JourneyFilter = journeyFilter;
        }


        /// <summary>
        /// If the given journey is pareto-optimal in respect to the current frontier,
        /// the journey is added.
        /// If this journey outperforms some other point on the frontier, that point is removed
        /// </summary>
        /// <param name="considered"></param>
        /// <returns>True if the journey was appended to the frontier</returns>
        public bool AddToFrontier(Journey<T> considered)
        {
            if (considered == null || ReferenceEquals(considered, Journey<T>.InfiniteJourney))
            {
                return false;
            }

            if (JourneyFilter != null && !JourneyFilter.CanBeTakenBackwards(considered))
            {
                return false;
            }

            /*
             * PCS runs backwards, thus starts at the latest departing journeys
             * This means that journeys which are added, will probably depart earlier and that
             * Frontier is sorted on Journey.Time, with the lowest (earliest) times to the end.
             * However, in a very few cases this order might be disturbed (mostly footpaths) because a footpath migth generate a walk
             * which is longer then another walk and arrive before another train is inserted.
             */

            for (var i = Frontier.Count - 1; i >= 0; i--)
            {
                var guard = Frontier[i];

                // ### About the ShadowIndex

                // The pareto frontier always uses a profiled connection
                // Thus, if the journey arrives sooner then every element in the frontier, it will be accepted regardless of other properties
                // The frontier is sorted by arrival time, with the latest arrival times first.

                // IN other words, we scan from the back of the list to the front 
                // As soon as the guard arrives later then considered, we know that no guard will be able to beat considered
                // However, the considered might still be able to kick out a few guards if it departs later then the guards
                // E.G. the following ascii art with journeys, departure to the left

                /* 
                 * Considered: (no transf)    |-------------->|
                 * Frontier[3] (one transf)      |------------>| (the last element of the frontier arrives later => Considered will be added to the frontier)
                 * Frontier[2] (no transf)   |-------------------->| (This one is worse then considered and should be removed)
                 * Frontier[1] ( 1 transf)                |-------------->| (This one is neither worse nor better then considered
                 * Frontier[0] (no transf)               |----------------->|    
                 *
                 * 
                 * How do we now we should still check up till Frontier[2]? For this we keep track of the departure time shadow.
                 * We shine a light at the bottom of the image above and register where the shadow falls:
                 *
                 * Considered: (no transf)   X|-------------->|
                 * Frontier[3] (one transf)  XXXX|------------>|
                 * Frontier[2] (no transf)   |-------------------->|
                 * Frontier[1] ( 1 transf)               X|-------------->|
                 * Frontier[0] (no transf)               |----------------->|    
                 *
                 *
                 * 'Light source:'         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                 * (X = shadow)
                 * As long as no light touches 'Considered', we have to continue scanning.
                 *
                 * In other words, we keep track what, for each frontier element, the index is of the element giving shadow to it)
                 * This is 'ShadowIndex'
                 */

                if (guard.Time > considered.Time)
                {
                    // At this point, we know that considered will be a part of the frontier
                    // We still have to check if there are elements on the frontier that could possibly be removed

                    if (ShadowIndex[i] > considered.Root.Time)
                    {
                        // At the current point, we know that for i (and all smaller indices holds):
                        // Frontier[i].(Arrival)Time > considered.Time -> Considered is accepted as it arrives sooner
                        // Frontier[i].Root.(Departure)Time > considered.Root.(DepartureTime) -> Considered can not remove a frontier element anymore, as it departs sooner
                        // IN other words: we are done with performing checks!
                        //    break;
                    }
                }


                var duel = Comparator.ADominatesB(guard, considered);
                switch (duel)
                {
                    case -1:
                        // The new journey didn't make the cut
                        return false;
                    case 1:
                        // The new journey defeated the guard
                        Frontier.RemoveAt(i);
                        ShadowIndex.RemoveAt(i);
                        i--;
                        FixShadowIndexFrom(i);
                        continue; // We continue the loop to remove other, possible sub-optimal entries further ahead in the list
                    case 0: // Both have exactly the same stats...
                        // Both are equally good
                        // They might be the same. We don't care about duplicates, so...
                        if (considered.Equals(guard))
                        {
                            return false;
                        }

                        // The new journey might take another subpath, e.g; travel via another station but arrive at the same time
                        // We add it here, but... as the guard is just as optimal,
                        // we know that no other journey can dominate it nor can this new journey dominate any other journey
                        // So we add the journey immediately and return
                        // Frontier.Add(considered);

                        // Also: because we know both are equally good, we can merge them (the pareto profile is kept only for if we'd transfer)
                        // And as a pareto frontier is only used in PCS, that should be fine
                        Frontier[i] = new Journey<T>(guard, considered);

                        return true;
                    case int.MaxValue: // Both are better then the other on some different statistic
                        // So: 1) The guard can not eliminate the candidate
                        // 2) The candidate can not eliminate the guard 
                        // We just have to continue scanning - if no guard defeats the candidate, it owned its place

                        continue;
                    default:
                        throw new Exception("Comparison of two journeys in metric did not return -1,1 or 0 but " +
                                            duel);
                }
            }

            var insertionPoint = Frontier.Count;
            while (insertionPoint > 0)
            {
                if (Frontier[insertionPoint - 1].Time >= considered.Time)
                {
                    // As it should be
                    // The list is already sorted, we are done
                    break;
                }

                _decreases++;
                // Hmm, we have to search further on!
                insertionPoint--;
            }

            // The new journey is on the pareto front and can be added
            if (insertionPoint < Frontier.Count)
            {
                _insertions++;
                Frontier.Insert(insertionPoint, considered);
                ShadowIndex.Add(uint
                    .MinValue); // This value does not matter, it'll be overwritten by FixShadowIndex anyway
                FixShadowIndexFrom(insertionPoint);
            }
            else if (insertionPoint == Frontier.Count)
            {
                Frontier.Add(considered);

                var earliest = considered.Root.Time;
                if (ShadowIndex.Count > 0)
                {
                    var lastShadowIndex = ShadowIndex[ShadowIndex.Count - 1];
                    if (lastShadowIndex < earliest)
                    {
                        earliest = lastShadowIndex;
                    }
                }

                ShadowIndex.Add(earliest);
            }
            else
            {
                throw new Exception("Wut?");
            }

            return true;
        }

        private uint _insertions;
        private uint _decreases;
        internal void DumpCounts()
        {
            Log.Information($"Insertions: {_insertions}, decreases: {_decreases}");
        }
        internal void IsSorted()
        {
            var lastDep = Frontier[0];
            foreach (var journey in Frontier)
            {
                if (lastDep.Time >= journey.Time)
                {
                    lastDep = journey;
                }
                else
                {
                    throw new Exception("Not sorted. A journey departs earlier then its predecessor");
                }
            }
        }

        private void FixShadowIndexFrom(int i)
        {
            if (Frontier.Count == 0 || Frontier.Count >= i)
            {
                return;
            }

            var curEarliest = Frontier[i];
            for (; i < Frontier.Count; i++)
            {
                if (Frontier[i].Root.Time < curEarliest.Root.Time)
                {
                    // This journey generates more shadow
                    curEarliest = Frontier[i];
                }

                ShadowIndex[i] = curEarliest.Root.Time;
            }
        }

        /// <summary>
        /// Considers all of the journeys to append them to the frontier.
        /// Returns all journeys which were added to the frontier
        ///
        /// IMPORTANT: Make sure to consume the iterator! Otherwise the 'yield returns' won't execute everything
        /// 
        /// </summary>
        public IEnumerable<Journey<T>> AddAllToFrontier(IEnumerable<Journey<T>> journeys)
        {
            foreach (var journey in journeys)
            {
                var wasAdded = AddToFrontier(journey);
                if (wasAdded)
                {
                    yield return journey;
                }
            }
        }


        public override string ToString()
        {
            var result = $"Pareto frontier with {Frontier.Count} entries";

            foreach (var j in Frontier)
            {
                result += "\n" + j;
            }

            return result;
        }
    }
}