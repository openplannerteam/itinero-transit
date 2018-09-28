using System;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.LinkedData
{
    public class Station : LinkedObject
    {

        /// <summary>
        /// Human readable name of the station
        /// </summary>
        public string Name { get; set; }
        public Uri Country { get; set; }

        // This is how geonames calls it too
        private static readonly Uri KingdomOfBelgium = new Uri("http://sws.geonames.org/2802361");

        public Station(Uri uri) : base(uri)
        {
        }

        public Station(JToken json) : base(new Uri(json["@id"].ToString()))
        {
            FromJson(json);
        }


        public override string ToString()
        {
            return
                $"Station {Name} ({Uri})";
        }

        protected sealed override void FromJson(JToken json)
        {
            Name = json["name"].ToString();
            Country = AsUri(json["country"].ToString());
        }


        public bool IsBelgian()
        {
            return Country.Equals(KingdomOfBelgium);
        }

    }
}