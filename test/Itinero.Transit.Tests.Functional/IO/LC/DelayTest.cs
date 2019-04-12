using System;
using System.IO;
using Itinero.Transit.IO.LC.Data;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// One issue that seems to exist is lacking delay data
    /// Here, we load a bunch of LC-jsons and see if delays are coming through
    /// </summary>
    public class DelayTest : FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
        {
            var contents = File.ReadAllText("connections0.json");

            var dloader = new Downloader();
            dloader.AlwaysReturn = contents;


            var loader = new JsonLdProcessor(dloader, new Uri("http://irail.be"));
            var expandedJson = loader.LoadExpanded(new Uri("http://irail.be/connections0"));
            var tt = new TimeTable((JObject) expandedJson);
            
            // Count the number of departure-delayed connections

            uint delayed = 0;
            foreach (var connection in tt.Connections())
            {
                if (connection.DepartureDelay > 0)
                {
                    delayed++;
                }
            }

            
            // According to a quick grep on connections 0, there should be 271 delayed connections
            True(271 == delayed);

            return true;
        }
    }
}