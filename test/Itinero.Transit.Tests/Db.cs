using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests
{
    internal static class Db
    {
        public static TransitDb GetDefaultTestDb()
        {
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();
            
            writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            writer.AddOrUpdateStop("https://example.com/stops/0", 0.1, 0.1);

            writer.AddOrUpdateConnection(((uint) 0, (uint) 0), ((uint) 0, (uint) 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00),
                10 * 60, 0,0, 0);
            
            writer.AddOrUpdateConnection(((uint) 0, (uint) 1), ((uint) 0, (uint) 2),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00),
                10 * 60, 0,0, 1);

            writer.AddOrUpdateConnection(((uint) 0, (uint) 2), ((uint) 0, (uint) 3),
                "https://example.com/connections/3",
                new DateTime(2018, 12, 04, 16, 46, 00),
                10 * 60, 0,0, 1);

            // Continues trip 0
            writer.AddOrUpdateConnection(((uint) 0, (uint) 1), ((uint) 0, (uint) 3),
                "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 16, 35, 00),
                40 * 60, 0,0, 0);
            
            // We add a very early and late connection in order to be able to run the algos and not run out of connections
            writer.AddOrUpdateConnection(((uint) 0, (uint) 10), ((uint) 0, (uint) 11),
                "https://example.com/connections/100",
                new DateTime(2018, 12, 04, 23, 30, 00),
                120, 0,0, 100
            );
            
            writer.AddOrUpdateConnection(((uint) 0, (uint) 11), ((uint) 0, (uint) 10),
                "AddOrUpdateConnection://example.com/connections/101",
                new DateTime(2018, 12, 04, 00, 30, 00),
                120, 0,0, 100
            );
            
            writer.Close();

            return transitDb;
        }

        public static IConnection GetConn(this TransitDb.TransitDbSnapShot db, uint id)
        {
            var reader = db.ConnectionsDb.GetReader();
            reader.MoveTo(id);
            return reader;
        }
    }
}