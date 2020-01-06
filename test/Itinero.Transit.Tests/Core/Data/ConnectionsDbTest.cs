using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Data.Simple;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class ConnectionsDbTest
    {
        [Fact]
        public void ConnectionsDb_WriteTo_ReadFrom_ExpectsSameResult()
        {
            var connsDb = new SimpleConnectionsDb(1);
            
            

            var inputConnection = new Connection(
                "XYZ", new StopId(1, 0), new StopId(1, 7),
                123456, 123, 2, 5, 3, new TripId(1, 2));
            connsDb.AddOrUpdate(inputConnection);

            using (var f = File.OpenWrite("Test.transitdb"))
            {
                f.Serialize(connsDb, new BinaryFormatter());
            }

            List<(ConnectionId, Connection)> read;
            using (var f = File.OpenRead("Test.transitdb"))
            {
                read = f.Deserialize<ConnectionId, Connection>(new BinaryFormatter()).OrderBy(c => c.Item2.DepartureTime).ToList();
            }

            File.Delete("Test.transitdb");
            

           Assert.Equal(inputConnection, read[0].Item2);
        }

        [Fact]
        public void EmptyConnections_EnumerateForward_NoCrash()
        {
            var connDb = new SimpleConnectionsDb(0);
            var enumerator = connDb.GetEnumeratorAt(0);

            while (enumerator.MoveNext())
            {
                throw new Exception("Nothing loaded?");
            }
        }

        [Fact]
        public void EmptyConnections_EnumerateBackward_NoCrash()
        {
            var connDb = new SimpleConnectionsDb(0);
            var enumerator = connDb.GetEnumeratorAt(0);

            while (enumerator.MovePrevious())
            {
                throw new Exception("Nothing loaded?");
            }
        }

        [Fact]
        public void ConnectionsDbWith3SConnections_EnumerateForward_AssumeRightOrder()
        {
            var connDb = new SimpleConnectionsDb(0);
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);
            var stop3 = new StopId(0, 3);

            var trip0 = new TripId(0, 0);

            connDb.Add(new Connection("0", stop0, stop1, 1000, 100, trip0));
            connDb.Add(new Connection("1", stop1, stop2, 1200, 100, trip0));
            connDb.Add(new Connection("2", stop2, stop3, 1400, 1000, trip0));


            var enumerator = connDb.GetEnumeratorAt(0);

            for (uint i = 0; i < 3; i++)
            {
                enumerator.MoveNext();
                var id = enumerator.Current;
                Assert.Equal(i, id.LocalId);
            }
        }

        [Fact]
        public void ConnectionsDbWith3SConnections_EnumerateBackwards_AssumeRightOrder()
        {
            var connDb = new SimpleConnectionsDb(0);
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);
            var stop3 = new StopId(0, 3);

            var trip0 = new TripId(0, 0);

            connDb.Add(new Connection("0", stop0, stop1, 1000, 100, trip0));
            connDb.Add(new Connection("1", stop1, stop2, 1200, 100, trip0));
            connDb.Add(new Connection("2", stop2, stop3, 1400, 1000, trip0));


            var enumerator = connDb.GetEnumeratorAt(0);

            for (uint i = 0; i < 3; i++)
            {
                enumerator.MoveNext();
                var id = enumerator.Current;
                Assert.Equal(i, id.LocalId);
            }
        }

        [Fact]
        public void ConnectionsDbWith3SameTimeConnections_EnumerateForward_AssumeRightOrder()
        {
            var connDb = new SimpleConnectionsDb(0);
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);
            var stop3 = new StopId(0, 3);

            var trip0 = new TripId(0, 0);

            connDb.Add(new Connection("0", stop0, stop1, 1000, 0, trip0));
            connDb.Add(new Connection("1", stop1, stop2, 1000, 0, trip0));
            connDb.Add(new Connection("2", stop2, stop3, 1000, 1000, trip0));


            var enumerator = connDb.GetEnumeratorAt(0);

            for (uint i = 0; i < 3; i++)
            {
                enumerator.MoveNext();
                var id = enumerator.Current;
                Assert.Equal(i, id.LocalId);
            }
        }

        [Fact]
        public void ConnectionsDbWith3SameTimeConnections_EnumerateBackwards_AssumeRightOrder()
        {
            var connDb = new SimpleConnectionsDb(0);
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);
            var stop3 = new StopId(0, 3);

            var trip0 = new TripId(0, 0);

            connDb.Add(new Connection("0", stop0, stop1, 1000, 0, trip0));
            connDb.Add(new Connection("1", stop1, stop2, 1000, 0, trip0));
            connDb.Add(new Connection("2", stop2, stop3, 1000, 1000, trip0));


            var enumerator = connDb.GetEnumeratorAt(2000);

            for (var i = 2; i >= 0; i--)
            {
                enumerator.MovePrevious();
                var id = enumerator.Current;
                Assert.Equal(i, (int) id.LocalId);
            }
        }
    }
}