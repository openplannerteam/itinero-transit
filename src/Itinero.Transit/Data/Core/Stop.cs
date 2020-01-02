using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.Core
{
    /// <summary>
    /// Representation of a stop.
    /// </summary>
    [Serializable]
    public class Stop : IGlobalId
    {
        /// <summary>
        /// Gets the global id.
        /// </summary>
        public string GlobalId { get; }

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
        public Dictionary<string, string> Attributes { get; }

        private static Dictionary<string, string> _empty = new Dictionary<string, string>();

        public Stop(Stop stop)
        {
            GlobalId = stop.GlobalId;
            Longitude = stop.Longitude;
            Latitude = stop.Latitude;
            Attributes = stop.Attributes != null ? new Dictionary<string, string>(stop.Attributes) : _empty;
        }

        public Stop(string globalId,
            (double longitude, double latitude) c, Dictionary<string, string> attributes = null)
        {
            GlobalId = globalId;
            Longitude = c.longitude;
            Latitude = c.latitude;
            Attributes = attributes ?? _empty;
        }


        [Pure]
        public override string ToString()
        {
            return $"{GlobalId} ([{Longitude},{Latitude}]) {Attributes}";
        }

        [Pure]
        public bool Equals(Stop other)
        {
            return GlobalId.Equals(other.GlobalId);
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Stop) obj);
        }

        [Pure]
        public override int GetHashCode()
        {
            return GlobalId.GetHashCode();
        }

        /// <summary>
        /// Returns the name of this stop, if 'name' is given in the attributes.
        /// If missing, the empty string is returned
        /// </summary>
        /// <returns></returns>
        [Pure]
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