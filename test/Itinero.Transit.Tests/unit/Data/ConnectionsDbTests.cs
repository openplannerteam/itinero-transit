// The MIT License (MIT)

// Copyright (c) 2018 Anyways B.V.B.A.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Itinero.Transit.Data;
using Xunit;

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
    }
}