using System;
using Itinero.Transit.Data;

namespace Itinero.IO.LC.Tests
{
    public static class Db
    {
        public static ConnectionsDb GetDefaultTestDb()
        {
            var connDb = new ConnectionsDb();
            connDb.Add(((uint) 0, (uint) 0), ((uint) 0, (uint) 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00),
                10 * 60, 0);



            
            
            connDb.Add(((uint) 0, (uint) 1), ((uint) 0, (uint) 2),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00),
                10 * 60, 1);

            connDb.Add(((uint) 0, (uint) 2), ((uint) 0, (uint) 3),
                "https://example.com/connections/3",
                new DateTime(2018, 12, 04, 16, 46, 00),
                10 * 60, 1);

            // Continues trip 0
            connDb.Add(((uint) 0, (uint) 1), ((uint) 0, (uint) 3),
                "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 16, 35, 00),
                40 * 60, 0);


            
            
            
            
            // We add a very early and late connection in order to be able to run the algos and not run out of connections
            connDb.Add(((uint) 0, (uint) 10), ((uint) 0, (uint) 11),
                "https://example.com/connections/100",
                new DateTime(2018, 12, 04, 23, 30, 00),
                120, 100
            );
            
            connDb.Add(((uint) 0, (uint) 11), ((uint) 0, (uint) 10),
                "https://example.com/connections/101",
                new DateTime(2018, 12, 04, 00, 30, 00),
                120, 100
            );
            
            return connDb;
        }

        

        public static Transit.Data.IConnection GetConn(this ConnectionsDb db, uint id)
        {
            var reader = db.GetReader();
            reader.MoveTo(id);
            return reader;
        }

        public static StopsDb GetDefaultStopsDb()
        {
            var stopsDb = new StopsDb();

            stopsDb.Add("https://example.com/stops/0", 0, 0.0);
            stopsDb.Add("https://example.com/stops/0", 0.1, 0.1);


            return stopsDb;
        }
    }
}