using System;
using System.Collections.Generic;

namespace Itinero.Transit.Journeys
{
    /// <summary>
    /// This JourneyStatistic will attempt to optimize the journeys in the following way:
    ///
    /// 1) The total time walking is minimized
    /// and iff the same: 
    /// 2) The total time travelling in a vehicle is minimized
    /// and iff the same:
    /// 3) The smallest transfer time is maximized (e.g. if one journey has a transfer of 2' and one of 6', while another has 3' and 3', the second one is chosen.
    /// and iff the same and an importance list is given
    /// 4) The biggest stations to transfers are chosen (as bigger station often have better facilities)*
    ///    > Here again, the smallest stations are avoided
    /// 
    /// This statistic is meant to be used _after_ PCS in order to weed out multiple journeys which have an equal performance on total travel time and number of transfers.
    ///
    /// * Once upon a time, I was testing an early implementation. That EAS implementation gave me a transfer of 1h in Angleur
    /// (which is a small station), while I could have transfered in Liege as well (a big station with lots of facilities).
    ///
    /// The closest food I could find at 19:00 was around one kilometer away.
    /// 
    /// </summary>
    public class TravellingTimeMinimizer : IJourneyStats<TravellingTimeMinimizer>
    {
        private readonly uint _totalTimeWalking = 0;
        private readonly uint _totalTimeInVehicle = 0;
        private readonly uint _smallestTransfer = uint.MaxValue;
        private readonly uint _leastImportantTransferstation = uint.MaxValue;

        private readonly Dictionary<(uint, uint), uint> _importances;
        
        public static readonly Minimizer Minimize = new Minimizer();

        public TravellingTimeMinimizer(Dictionary<(uint, uint), uint> importances)
        {
            _importances = importances;
        }

        public TravellingTimeMinimizer() : this(null)
        {
        }

        public TravellingTimeMinimizer(Dictionary<(uint, uint), uint> importances,
            uint totalTimeWalking, uint totalTimeInVehicle, uint smallestTransfer, uint leastImportantTransferstation)
        {
            _importances = importances;
            _totalTimeWalking = totalTimeWalking;
            _totalTimeInVehicle = totalTimeInVehicle;
            _smallestTransfer = smallestTransfer;
            _leastImportantTransferstation = leastImportantTransferstation;
        }


        public TravellingTimeMinimizer EmptyStat()
        {
            return new TravellingTimeMinimizer(_importances);
        }

        public TravellingTimeMinimizer Add(Journey<TravellingTimeMinimizer> journey)
        {
            var totalTimeWalking = _totalTimeWalking;
            var totalTimeInVehicle = _totalTimeInVehicle;
            var smallestTransfer = _smallestTransfer;
            var leastImportantTransferstation = _leastImportantTransferstation;

            var journeyTime = (uint) (journey.ArrivalTime() - journey.DepartureTime());
            
            if (journey.SpecialConnection && journey.Connection == Journey<TravellingTimeMinimizer>.WALK)
            {
                totalTimeWalking += journeyTime;
            }else if (journey.SpecialConnection && journey.Connection == Journey<TravellingTimeMinimizer>.TRANSFER)
            {
                smallestTransfer = Math.Min(smallestTransfer, journeyTime);

                var importance = leastImportantTransferstation;
                if (_importances?.TryGetValue(journey.Location, out importance) ?? false)
                {
                    leastImportantTransferstation = Math.Min(leastImportantTransferstation, importance);
                }
                
            }else if (!journey.SpecialConnection)
            {
                // We simply are travelling in a vehicle
                totalTimeInVehicle += journeyTime;
            }

            return new TravellingTimeMinimizer(_importances, totalTimeWalking, totalTimeInVehicle, smallestTransfer, leastImportantTransferstation);
        }
        
        
        public class Minimizer : StatsComparator<TravellingTimeMinimizer>
        {
            public override int ADominatesB(Journey<TravellingTimeMinimizer> ja, Journey<TravellingTimeMinimizer> jb)
            {

                var a = ja.Stats;
                var b = jb.Stats;

                if (a._totalTimeWalking != b._totalTimeWalking)
                {
                    return a._totalTimeWalking.CompareTo(b._totalTimeWalking);
                }
 
                if (a._totalTimeInVehicle != b._totalTimeInVehicle)
                {
                    return a._totalTimeInVehicle.CompareTo(b._totalTimeInVehicle);
                }
               
                if (a._smallestTransfer != b._smallestTransfer)
                {
                    // SHOULD BE MAXIMIZED! 
                    return b._smallestTransfer.CompareTo(a._smallestTransfer);
                }

                if (a._leastImportantTransferstation != b._leastImportantTransferstation)
                {
                    // SHOULD BE MAXIMIZED
                    return b._leastImportantTransferstation.CompareTo(a._leastImportantTransferstation);
                }

                return 0;
            }
        }
    }
}