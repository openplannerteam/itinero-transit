using Itinero.Transit.Data.Attributes;

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
            if (this.Attributes != null)
            {
                this.Attributes = new AttributeCollection(this.Attributes);
            }
        }
        
        internal Stop(string globalId, (uint tileId, uint localId) id,
            double longitude, double latitude, IAttributeCollection attributes)
        {
            this.GlobalId = globalId;
            this.Id = id;
            this.Longitude = longitude;
            this.Latitude = latitude;
            if (attributes != null)
            {
                this.Attributes = new AttributeCollection(this.Attributes);
            }
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
        
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public IAttributeCollection Attributes { get; }

        public override string ToString()
        {
            return $"{this.GlobalId} ({this.Id}-[{this.Longitude},{this.Latitude}]) {this.Attributes}";
        }
    }
}