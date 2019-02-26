using Reminiscence.Collections;

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

        private uint _totalTimeWalking = 0;
        private uint _totalTimeInVehicle = 0;
        private uint _smallestTransfer = uint.MaxValue;

        private readonly Dictionary<(uint, uint), uint> _importances;

        public TravellingTimeMinimizer(Dictionary<(uint, uint), uint> importances)
        {
            _importances = importances;
        }

        public TravellingTimeMinimizer():this(null)
        {
            
        }


        public TravellingTimeMinimizer EmptyStat()
        {
            return new TravellingTimeMinimizer(_importances);
        }

        public TravellingTimeMinimizer Add(Journey<TravellingTimeMinimizer> journey)
        {
            throw new System.NotImplementedException();
        }
    }
}