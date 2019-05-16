using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO
{
    /// <summary>
    /// Tests loading of an osm relation
    /// </summary>
    public class OsmTest : FunctionalTest<bool, string>
    {
        public const string PRGent = "https://www.openstreetmap.org/relation/9508548";
        public const string ShuttleBrugge = "9413958";

        public static List<string> TestRelations = new[]
        {
            PRGent,
            ShuttleBrugge
        }.ToList();

        protected override bool Execute(string input)
        {
            var tdb = new TransitDb();
            tdb.UseOsmRoute(input, DateTime.Now.Date, DateTime.Now.Date.AddDays(1));
            return true;
        }
    }
}