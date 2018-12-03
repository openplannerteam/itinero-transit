using System;
using System.Collections.Generic;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero.IO.LC{
    
    
    /// <summary>
    /// The linked connections catalog is responsible for parsing a catalog.
    /// A catalog contains links to the providers of connections.
    /// These connection provider are in turn returned by this object
    /// </summary>
    public class LinkedConnectionsCatalog : LinkedObject
    {
        public readonly List<Uri> Catalogae = new List<Uri>();
        
        public LinkedConnectionsCatalog(Uri uri) : base(uri)
        {
        }
        
        protected override void FromJson(JObject json)
        {
            // The catalog contains the links where one can find the timetables of a single operator
            // The operator might be split based on geographical area (e.g. De Lijn)
            // The timetables can be found within dcat:dataset

            var dataset = json["http://www.w3.org/ns/dcat#dataset"][0]
                                ["http://www.w3.org/ns/dcat#distribution"];

            foreach (var area in dataset)
            {
                var accessUri = area.GetLDValue("http://www.w3.org/ns/dcat#accessURL");
                Catalogae.Add(new Uri(accessUri));
            }
            
        }
    }
}