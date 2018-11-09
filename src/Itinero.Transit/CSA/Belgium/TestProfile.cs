using System;
using System.Collections.Generic;

namespace Itinero.Transit.Belgium
{
    public class TestProfile
    {
        private readonly DateTime _testDay;


        public TestProfile(DateTime testDay)
        {
            _testDay = testDay;
        }

        public DateTime Moment(int hour, int minute, int seconds = 0)
        {
            return new DateTime(_testDay.Year, _testDay.Month, _testDay.Day,
                hour, minute, seconds);
        }

        public readonly static Uri a = new Uri("http://example.com/location/A");
        public readonly static Uri b = new Uri("http://example.com/location/B");
        public readonly static Uri c = new Uri("http://example.com/location/C");
        public readonly static Uri d = new Uri("http://example.com/location/D");

        public Profile<TransferStats> CreateTestProfile()
        {
            var trainConn = new LinkedConnection(new Uri("http://example.com/conn/1"))
            {
                DepartureStop = c, ArrivalStop = d, DepartureTime = Moment(18, 00), ArrivalTime = Moment(19, 00)
            };

            var busConn = new LinkedConnection(new Uri("http://example.com/conn/2"))
            {
                DepartureStop = a, ArrivalStop = b, DepartureTime = Moment(17, 00), ArrivalTime = Moment(17, 45)
            };

            var tt = new SimpleTimeTable(new List<IConnection> {busConn, trainConn});
            var conProv = new SimpleConnProvider(tt);


            var locA = new Location(a)
            {
                Name = "A",
                Lat = 51.0f,
                Lon = 3.0f
            };


            var locB = new Location(b)
            {
                Name = "b",
                Lat = 51.21293f,
                Lon = 3.21870f
                
            };

            var locC = new Location(c)
            {
                Name = "C",
                Lat =  51.21635f,
                Lon = 3.21971f
            };

            var locD = new Location(d)
            {
                Name = "D",
                Lat = 53.0f,
                Lon = 5.0f
            };
            var locProv = new LocationsFragment(new Uri("http://example.com/locations/overview"),
                new List<Location> {locA, locB, locC, locD});
            var profile = new Profile<TransferStats>(
                conProv,
                locProv,
                new TransferGenerator("belgium.routerdb"),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare,
                TransferStats.ParetoCompare
            );

            return profile;
        }
    }
}