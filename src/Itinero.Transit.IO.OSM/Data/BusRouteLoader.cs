namespace Itinero.Transit.Data
{
    /// <summary>
    /// Loads a PTv2-route relation from OSM, and adds it to a transitDB (for a given timerange)
    /// </summary>
    public class BusRouteLoader
    {

        private uint _relationId;

        public BusRouteLoader(uint relationId)
        {
            _relationId = relationId;
            
            
            
        }   
    }
}