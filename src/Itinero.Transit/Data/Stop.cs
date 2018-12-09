namespace Itinero.Transit.Data
{
    /// <summary>
    /// Representation of a stop.
    /// </summary>
    public class Stop : IStop
    {
        internal Stop(IStop stop)
        {
            this.GlobalId = stop.GlobalId;
            this.Id = stop.Id;
            this.Longitude = stop.Longitude;
            this.Latitude = stop.Latitude;
        }
        
        internal Stop(string globalId, (uint tileId, uint localId) id,
            double longitude, double latitude)
        {
            this.GlobalId = globalId;
            this.Id = id;
            this.Longitude = longitude;
            this.Latitude = latitude;
        }
        
        /// <summary>
        /// Gets the global id.
        /// </summary>
        public string GlobalId { get; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public (uint tileId, uint localId) Id { get; }

        /// <summary>
        /// Gets the longitude.
        /// </summary>
        public double Longitude { get; }

        /// <summary>
        /// Gets the latitude.
        /// </summary>
        public double Latitude { get; }

        public override string ToString()
        {
            return $"{this.GlobalId} ({this.Id}-[{this.Longitude},{this.Latitude}])";
        }
    }
}