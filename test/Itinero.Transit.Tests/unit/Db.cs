using System;
using Itinero.Transit.Data;

namespace Itinero.IO.LC.Tests
{
    public static class Db
    {
        public static ConnectionsDb GetDefaultTestDb()
        {
            var connDb = new ConnectionsDb();
            connDb.Add((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00),
                10 * 60, 0);

            connDb.Add((0, 1), (0, 2),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00),
                10 * 60, 1);
            return connDb;
        }


        public static Connection GetConn(this ConnectionsDb db, uint id)
        {
            var reader = db.GetReader();
            reader.MoveTo(id);
            return reader;
        }

        public static StopsDb GetDefaultStopsDb()
        {
            var stopsDb = new StopsDb();

            var id = stopsDb.Add("https://example.com/stops/0", 0, 0.0);
            stopsDb.Add("https://example.com/stops/0", 0.1, 0.1);


            return stopsDb;
        }
    }
}