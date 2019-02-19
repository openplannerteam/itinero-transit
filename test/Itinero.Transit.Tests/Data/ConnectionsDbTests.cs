using System;
using System.IO;
using Itinero.Transit.Data;
using Xunit;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedVariable

namespace Itinero.Transit.Tests.Data
{
    public class ConnectionsDbTests
    {
        [Fact]
        public void ConnectionsDb_ShouldStoreConnection()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);
            
            Assert.Equal((uint)0, id);
        }
        
        [Fact]
        public void ConnectionsDbReader_ShouldMoveToConnectionByInternalId()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);

            var reader = db.GetReader();
            Assert.True(reader.MoveTo(id));
        }
        
        [Fact]
        public void ConnectionsDbReader_ShouldMoveToConnectionByGlobalId()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);

            var reader = db.GetReader();
            Assert.True(reader.MoveTo("http://irail.be/connections/8813003/20181216/IC1545"));
            Assert.Equal((uint)10245, reader.TripId);
        }
        
        [Fact]
        public void ConnectionsDbReader_ShouldReturnGlobalId()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);

            var reader = db.GetReader();
            reader.MoveTo(id);
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", reader.GlobalId);
        }
        
        [Fact]
        public void ConnectionsDbReader_ShouldReturnTripId()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);

            var reader = db.GetReader();
            reader.MoveTo(id);
            Assert.Equal((uint)10245, reader.TripId);
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionByDeparture()
        {
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", departureTime, 1024, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByDeparture()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544", new DateTime(2018, 11, 14, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsInTheSameMinuteByDeparture()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 09), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",  new DateTime(2018, 11, 14, 2, 3, 07), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 2, 3, 35), 3600, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 2, 3, 01), 4500, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByArrival()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",  new DateTime(2018, 11, 14, 2, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 9), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 2, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 2, 3, 9), 3600, 10245);

            var enumerator = db.GetArrivalEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByDepartureOnDifferentDates()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 4, 09), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",  new DateTime(2018, 11, 15, 2, 6, 07), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 16, 2, 8, 35), 3600, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 15, 2, 9, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 2,10, 01), 4500, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByArrivalOnDifferentDates()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 4, 09), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",  new DateTime(2018, 11, 15, 2, 6, 07), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 16, 2, 8, 35), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 15, 2, 9, 10), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 2,10, 01), 1024, 10245);

            var enumerator = db.GetArrivalEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByDepartureInReverse()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544", new DateTime(2018, 11, 14, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            
            Assert.False(enumerator.MovePrevious());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByDepartureInReverseOnDifferentDates()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 4, 09), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",  new DateTime(2018, 11, 15, 2, 6, 07), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 16, 2, 8, 35), 3600, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 15, 2, 9, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 14, 2,10, 01), 4500, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            
            Assert.False(enumerator.MovePrevious());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldMoveNextToDateTimeAndEnumerateFromThere()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544", new DateTime(2018, 11, 13, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 15, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext(new DateTime(2018, 11, 13)));
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
           
            Assert.True(enumerator.MoveNext(new DateTime(2018, 11, 13, 4, 0, 0)));
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
           
            Assert.True(enumerator.MoveNext(new DateTime(2018, 11, 15, 4, 0, 0)));
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void ConnectionsDbEnumerator_ShouldMovePreviousToDateTimeAndEnumerateFromThere()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544", new DateTime(2018, 11, 13, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 15, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 16)));
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());
            
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 11, 0, 0)));
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());
            
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 0, 0, 0)));
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());
        }

        [Fact]
        public void ConnectionsDb_CloneShouldBeCopy()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            db = db.Clone();

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 16)));
            Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());

            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 11, 0, 0)));
            Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());

            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 0, 0, 0)));
            Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());
        }

        [Fact]
        public void ConnectionsDb_WriteToReadFromShouldBeCopy()
        {
            var db = new ConnectionsDb(60);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813003/20181216/IC1545", new DateTime(2018, 11, 14, 2, 3, 9), 1024, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8892056/20181216/IC544", new DateTime(2018, 11, 13, 4, 3, 9), 54, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8821311/20181216/IC1822", new DateTime(2018, 11, 14, 2, 3, 10), 102, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8813045/20181216/IC3744", new DateTime(2018, 11, 15, 5, 3, 9), 4500, 10245);
            db.Add((100, 0), (100, 1), "http://irail.be/connections/8812005/20181216/S11793", new DateTime(2018, 11, 14, 10, 3, 35), 3600, 10245);

            using (var stream = new MemoryStream())
            {
                var size = db.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                db = ConnectionsDb.ReadFrom(stream);
                
                var enumerator = db.GetDepartureEnumerator();
                Assert.NotNull(enumerator);
                Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 16)));
                Assert.Equal("http://irail.be/connections/8813045/20181216/IC3744", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
                Assert.False(enumerator.MovePrevious());
            
                Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 11, 0, 0)));
                Assert.Equal("http://irail.be/connections/8812005/20181216/S11793", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8821311/20181216/IC1822", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);
                Assert.True(enumerator.MovePrevious());
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
                Assert.False(enumerator.MovePrevious());
            
                Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 14, 0, 0, 0)));
                Assert.Equal("http://irail.be/connections/8892056/20181216/IC544", enumerator.GlobalId);
                Assert.False(enumerator.MovePrevious());
            }
        }
    }
}