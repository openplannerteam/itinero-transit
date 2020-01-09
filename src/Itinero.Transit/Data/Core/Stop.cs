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
        public IReadOnlyDictionary<string, string> Attributes { get; }

        public Stop(Stop stop)
        {
            GlobalId = stop.GlobalId;
            Longitude = stop.Longitude;
            Latitude = stop.Latitude;
            var attributes = new Dictionary<string, string>();

            if (stop.Attributes != null)
            {
                foreach (var kv in stop.Attributes)
                {
                    attributes[kv.Key] = kv.Value;
                }
            }

            Attributes = attributes;
        }

        public Stop(string globalId,
            (double longitude, double latitude) c, IReadOnlyDictionary<string, string> attributes = null)
        {
            GlobalId = globalId;
            (Longitude, Latitude) = c;
            Attributes = attributes;
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