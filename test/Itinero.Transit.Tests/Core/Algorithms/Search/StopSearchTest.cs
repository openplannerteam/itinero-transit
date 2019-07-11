using System;
using System.Linq;
using Itinero.Transit.Data;
using Xunit;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Aggregators;

namespace Itinero.Transit.Tests.Core.Algorithms.Search
{
    public class StopSearchTest
    {
        public static (StopsDb.StopsDbReader, StopId howest, StopId sintClara, StopId station)
            CreateTestReader()
        {
            var tdb = new TransitDb();

            var wr = tdb.GetWriter();


            var howest = wr.AddOrUpdateStop("howest", 3.22121f, 51.21538f);
            // Around 100m further
            var sintClara = wr.AddOrUpdateStop("sint-clara", 3.2227f, 51.2153f);

            var station = wr.AddOrUpdateStop("station-brugge", 3.21782f, 51.19723f);

            wr.Close();

            return (tdb.Latest.StopsDb.GetReader(), howest, sintClara, station);
        }


        [Fact]
        public void TestDistance()
        {
            var distanceHowest = StopSearch.DistanceEstimateInMeter(3.22121f, 51.21538f, 3.2227f, 51.2153f);
            Assert.True(100 < distanceHowest && distanceHowest < 110);

            var distanceStation = StopSearch.DistanceEstimateInMeter(3.21782f, 51.19723f, 3.2227f, 51.2153f);
            Assert.True(2000 < distanceStation && distanceStation < 2100);

            var (stops, howest, sintClara, station ) = CreateTestReader();

            var d = StopSearch.CalculateDistanceBetween(stops, howest, sintClara);
            Assert.True(Math.Abs(distanceHowest - d) < 2.0); // There is a small rounding error somewhere


            d = StopSearch.CalculateDistanceBetween(stops, station, sintClara);
            Assert.True(Math.Abs(distanceStation - d) < 2.0); // There is a small rounding error somewhere
        }


        [Fact]
        public void TestClosest()
        {
            var (stops, howest, sintClara, station ) = CreateTestReader();

            Assert.Equal(sintClara, stops.FindClosest( 51.2153f,3.2227f).Id);

            Assert.Equal(howest, stops.FindClosest(51.21538f, 3.22121f).Id);
            Assert.Equal(howest,
                stops.FindClosest(51.21538f, 3.22111f).Id); //Slightly perturbated longitude

            Assert.Equal(station, stops.FindClosest( 51.19723f,3.21782f).Id);
            // Outside of maxDistance
            Assert.Null(stops.FindClosest( 51.19723f,3.0f));
        }

        [Fact]
        public void TestSearchInBox()
        {
            var (stops, howest, _, _ ) = CreateTestReader();

            var box = (3f, 51f, 4f, 52f);
            var found = stops.SearchInBox(box).ToList();
            Assert.Equal(3, found.Count());


            // a total corner case: near exact latitude of 'howest'
            box = (3.2212f, 51.21537f, 3.22122f, 51.21539f);
            found = stops.SearchInBox(box).ToList();
            Assert.Single(found);
            Assert.Equal(howest, found[0].Id);
        }

        [Fact]
        public void StopsSearch_ShouldEnumerateAllInBBox()
        {
            var db = new StopsDb(0);
            db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var stops = db.GetReader().SearchInBox((4.64, 51.17, 5.54, 51.38));
            Assert.NotNull(stops);

            var stopsList = stops.ToList();
            Assert.Equal(6, stopsList.Count);
        }

        [Fact]
        public void StopsSearch_ShouldFindClosest()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var stop = db.GetReader().FindClosest(51.26277419739382, 4.78686332702636);
            Assert.NotNull(stop);
            Assert.Equal(id1, stop.Id);
        }


        [Fact]
        public void StopsSearchCachedTest()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var reader = db.GetReader().UseCache();
            var stop = reader.FindClosest( 51.26277419739382, 4.78686332702636);
            Assert.NotNull(stop);
            var stop0 = reader.FindClosest( 51.26277419739382, 4.78686332702636);
            Assert.Equal(id1, stop.Id);
            Assert.Equal(id1, stop0.Id);
        }
    }
}