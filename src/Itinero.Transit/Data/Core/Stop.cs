using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Core
{
    /// <inheritdoc />
    /// <summary>
    /// Representation of a stop.
    /// </summary>
    public class Stop : IStop
    {
        public Stop(IStop stop)
        {
            GlobalId = stop.GlobalId;
            Id = stop.Id;
            Longitude = stop.Longitude;
            Latitude = stop.Latitude;
            if (Attributes != null)
            {
                Attributes = new AttributeCollection(Attributes);
            }
        }
        
        internal Stop(string globalId, StopId id,
            double longitude, double latitude, IAttributeCollection attributes)
        {
            GlobalId = globalId;
            Id = id;
            Longitude = longitude;
            Latitude = latitude;
            if (attributes != null)
            {
                Attributes = new AttributeCollection(Attributes);
            }
        }

        public Stop(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Id= StopId.Invalid;
        }

        /// <summary>
        /// Gets the global id.
        /// </summary>
        public string GlobalId { get; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public StopId Id { get; }

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
            return $"{GlobalId} ({Id}-[{Longitude},{Latitude}]) {Attributes}";
        }

        protected bool Equals(Stop other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Stop) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns the name of this stop, if 'name' is given in the attributes.
        /// If missing, the empty string is returned
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            if (Attributes == null)
            {
                return "";
            }
            Attributes.TryGetValue("name", out var name);
            return name ?? "";
        }
    }
}