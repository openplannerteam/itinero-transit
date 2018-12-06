using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using OsmSharp.IO.PBF;
using Serilog;

namespace Itinero.Transit.Tests.Functional.Tests
{
    public class AesTest : FunctionalTest
    {
        public override void Test()
        {
            var conns = GetTestDb().conns;
            var stopsDb = GetTestDb().stops;
            var stopIds = GetTestDb().mapping;

            var p = new Profile<TransferStats>(
                conns, stopsDb,
                new NoWalksGenerator(), new TransferStats());

            var depTime = DateTime.Now.Date.AddHours(10);

         /*   
            var eas = new EarliestConnectionScan<TransferStats>(
                stopIds[Brugge], stopIds[Gent],
               depTime.ToUnixTime(), depTime.AddHours(3).ToUnixTime(), p);

            
            Log.Information($"Brugge is {stopIds[Brugge]}");

            
            
            
            EarliestConnectionScan<TransferStats>.StartPoint = stopIds[Brugge];
           */
            Journey<TransferStats> journey = null; //eas.CalculateJourney();
            if (journey == null)
            {
                throw new Exception("No journey found");
            }
        }
    }
}