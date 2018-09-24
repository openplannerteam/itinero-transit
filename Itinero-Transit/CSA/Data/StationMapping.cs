using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.LinkedData
{
    /**
     * A linked object is an object which has an Uniform Resource Identifier
     */
    public abstract class StationMapping
    {
        public Dictionary<string, string> mapping = new Dictionary<string, string>();

        public StationMapping()
        {
            mapping.Add("http://irail.be/stations/NMBS/008814001", "Brussel-Zuid");
            
        }

        public string station(Uri uri)
        {
            if (mapping.ContainsKey(uri.OriginalString))
            {
                return mapping[uri.OriginalString];
            }
            return "";
        }
    }
}