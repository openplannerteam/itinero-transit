using System;
using System.IO;
using Itinero.Transit.Data;
using OsmSharp.IO.PBF;
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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            Assert.Equal((uint) 0, id);
        }

        [Fact]
        public void ConnectionsDbReader_ShouldMoveToConnectionByInternalId()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            var reader = db.GetReader();
            Assert.True(reader.MoveTo(id));
        }

        [Fact]
        public void ConnectionsDbReader_ShouldMoveToConnectionByGlobalId()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            var reader = db.GetReader();
            Assert.True(reader.MoveTo("http://irail.be/connections/8813003/20181216/IC1545"));
            Assert.Equal((uint) 10245, reader.TripId);
        }

        [Fact]
        public void ConnectionsDbReader_ShouldReturnGlobalId()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            var reader = db.GetReader();
            reader.MoveTo(id);
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", reader.GlobalId);
        }

        [Fact]
        public void ConnectionsDbReader_ShouldReturnTripId()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            var reader = db.GetReader();
            reader.MoveTo(id);
            Assert.Equal((uint) 10245, reader.TripId);
        }

        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionByDeparture()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            var departureTime = new DateTime(2018, 11, 14, 2, 3, 9);
            var id = db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                departureTime.ToUnixTime(), 1024, 0, 0, 10245, 0);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("http://irail.be/connections/8813003/20181216/IC1545", enumerator.GlobalId);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void ConnectionsDbEnumerator_ShouldEnumerateConnectionsByDeparture()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 09).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 2, 3, 07).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 2, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 2, 3, 01).ToUnixTime(), 4500, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 4, 09).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 15, 2, 6, 07).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 16, 2, 8, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 15, 2, 9, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 2, 10, 01).ToUnixTime(), 4500, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 4, 09).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 15, 2, 6, 07).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 16, 2, 8, 35).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 15, 2, 9, 10).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 2, 10, 01).ToUnixTime(), 1024, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 4, 09).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 15, 2, 6, 07).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 16, 2, 8, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 15, 2, 9, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 14, 2, 10, 01).ToUnixTime(), 4500, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

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
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 13, 4, 3, 9).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8821311/20181216/IC1822",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 102, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813045/20181216/IC3744",
                new DateTime(2018, 11, 15, 5, 3, 9).ToUnixTime(), 4500, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8812005/20181216/S11793",
                new DateTime(2018, 11, 14, 10, 3, 35).ToUnixTime(), 3600, 0, 0, 10245, 0);

            using (var stream = new MemoryStream())
            {
                var size = db.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                db = ConnectionsDb.ReadFrom(stream, 0);

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

        [Fact]
        public void ConnectionsDbEnumerator_AddOrUpdate_ShouldUpdateTrips()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10246, 0);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 16)));
            Assert.Equal((uint) 10246, enumerator.TripId);
        }

        [Fact]
        public void ConnectionsDbEnumerator_AddOrUpdate_ShouldUpdateStops()
        {
            var loc3 = new LocationId(0, 100, 3);
            var loc2 = new LocationId(0, 100, 2);

            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc2, loc3, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MovePrevious(new DateTime(2018, 11, 16)));
            Assert.Equal((uint) 2, enumerator.DepartureStop.LocalId);
            Assert.Equal((uint) 3, enumerator.ArrivalStop.LocalId);
        }

        [Fact]
        public void ConnectionsDbEnumerator_AddOrUpdate_ShouldUpdateDepartureTimeWithinSameWindow()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 11).ToUnixTime(), 1024, 0, 0, 10245, 0);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext(new DateTime(2018, 11, 14)));
            Assert.Equal(new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), enumerator.DepartureTime);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(new DateTime(2018, 11, 14, 2, 3, 11).ToUnixTime(), enumerator.DepartureTime);
        }

        [Fact]
        public void ConnectionsDbEnumerator_AddOrUpdate_ShouldUpdateDepartureTimeBetweenWindows()
        {
            var loc1 = new LocationId(0, 100, 1);
            var loc = new LocationId(0, 100, 0);
            var db = new ConnectionsDb(60);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 3, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8892056/20181216/IC544",
                new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), 54, 0, 0, 10245, 0);
            db.AddOrUpdate(loc, loc1, "http://irail.be/connections/8813003/20181216/IC1545",
                new DateTime(2018, 11, 14, 2, 4, 9).ToUnixTime(), 1024, 0, 0, 10245, 0);

            var enumerator = db.GetDepartureEnumerator();
            Assert.NotNull(enumerator);
            Assert.True(enumerator.MoveNext(new DateTime(2018, 11, 14)));
            Assert.Equal(new DateTime(2018, 11, 14, 2, 3, 10).ToUnixTime(), enumerator.DepartureTime);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(new DateTime(2018, 11, 14, 2, 4, 9).ToUnixTime(), enumerator.DepartureTime);
        }


        [Fact]
        public void ModeCopyTest()
        {
            var tdb = new TransitDb(0);

            var wr = tdb.GetWriter();

            var a = wr.AddOrUpdateStop("a", 0, 0, null);
            var b = wr.AddOrUpdateStop("b", 1, 1, null);

            var date = DateTime.Now;
            wr.AddOrUpdateConnection(a, b, "c", date, 600, 60, 120, 1, 1);
            wr.Close();


            var con = tdb.Latest.ConnectionsDb.GetReader();
            con.MoveTo(0);

            Assert.Equal(1, con.Mode);


            var nwTdb = new TransitDb(0);
 
            wr = nwTdb.GetWriter();

            a = wr.AddOrUpdateStop("a", 0, 0, null);
            b = wr.AddOrUpdateStop("b", 1, 1, null);

            wr.AddOrUpdateConnection(
                con.DepartureStop,
                con.ArrivalStop,
                con.GlobalId,
                con.DepartureTime.FromUnixTime(),
                con.TravelTime,
                con.DepartureDelay,
                con.ArrivalDelay,
                con.TripId,
                con.Mode
            );

            wr.Close();


            con = nwTdb.Latest.ConnectionsDb.GetReader();
            con.MoveTo(0);

            Assert.Equal(1, con.Mode);
        }
    }
}