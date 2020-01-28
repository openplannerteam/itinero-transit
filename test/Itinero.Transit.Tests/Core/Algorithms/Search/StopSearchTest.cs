using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;
using Itinero.Transit.Data.Simple;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.Search
{
    public class StopSearchTest
    {
        [Fact]
        public void LocationIndex_LocationAround00_LocationIsFound()
        {
            var index = new TiledLocationIndexing<string>();
            index.Add((0.000001, 0.000001), "found");
            var found = index.GetInRange((0, 0), 500);
            Assert.Single(found);
            Assert.Equal("found", found[0]);
        }

        [Fact]
        public void LocationIndex_LocationAround0101_LocationIsFound()
        {
            var index = new TiledLocationIndexing<string>();
            index.Add((0.0000001, 0.0000001), "found");
            var found = index.GetInRange((0, 0), 500);
            Assert.Single(found);
            Assert.Equal("found", found[0]);
        }

        [Fact]
        public void LocationIndex_LocationOn00_LocationIsFound()
        {
            var index = new TiledLocationIndexing<string>();
            index.Add((0, 0), "found");
            var found = index.GetInRange((0.0001, 0.0001), 500);
            Assert.Single(found);
            Assert.Equal("found", found[0]);
        }


        private static (IStopsDb, StopId howest, StopId sintClara, StopId station) CreateTestReader()
        {
            var tdb = new TransitDb(0);

            var wr = tdb.GetWriter();


            var howest = wr.AddOrUpdateStop(new Stop("howest", (3.22121, 51.21538)));
            // Around 100m further
            var sintClara = wr.AddOrUpdateStop(new Stop("sint-clara", (3.2227, 51.2153)));

            var station = wr.AddOrUpdateStop(new Stop("station-brugge", (3.21782, 51.19723)));
            tdb.CloseWriter();

            return (tdb.Latest.Stops, howest, sintClara, station);
        }


        [Fact]
        public void CalculateDistanceBetween_MultipleStops_ExpectsCorrectDistances()
        {
            var distanceHowest = DistanceEstimate.DistanceEstimateInMeter(
                (3.22121f, 51.21538f), (3.2227f, 51.2153f));
            Assert.True(100 < distanceHowest && distanceHowest < 110);

            var distanceStation = DistanceEstimate.DistanceEstimateInMeter((3.21782f, 51.19723f), (3.2227f, 51.2153f));
            Assert.True(2000 < distanceStation && distanceStation < 2100);
        }


        [Fact]
        public void FindClosest_FewStopsInReader_ExpectsClosestStop()
        {
            var (stops, howest, sintClara, station ) = CreateTestReader();

            var found = stops.FindClosest((3.2227f, 51.2153f), 5000);

            Assert.Equal(stops.Get(sintClara), found);
            Assert.Equal(stops.Get(howest),
                stops.FindClosest((3.22121f, 51.21538f), 5000));
            Assert.Equal(stops.Get(howest),
                stops.FindClosest((3.22111, 51.21538), 5000)); //Slightly perturbated longitude

            Assert.Equal(stops.Get(station),
                stops.FindClosest(new Stop("a", (3.21782, 51.19723)), 5000));
            // Outside of maxDistance
            Assert.Null(stops.FindClosest(new Stop("b", (3.0, 51.19723)), 50));
        }


        [Fact]
        public void SearchInBox_SmallReader_Expects6Stops()
        {
            var db = new SimpleStopsDb(0);
            db.Add(new Stop("http://irail.be/stations/NMBS/008863354", (4.786863327026367, 51.262774197393820)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863008", (4.649276733398437, 51.345839804352885)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863009", (4.989852905273437, 51.223657764702750)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863010", (4.955863952636719, 51.325462944331300)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863011", (4.830207824707031, 51.373280620643370)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863012", (5.538825988769531, 51.177621156752494)));
            db.PostProcess(14);
            var stops = db.GetInRange((4, 51.275), 500000);

            Assert.NotNull(stops);

            Assert.Equal(6, stops.Count);
        }

        [Fact]
        public void FindClosest_SmallReader_ExpectsNo1()
        {
            var db = new SimpleStopsDb(0);
            var id1 =
                db.Add(new Stop("http://irail.be/stations/NMBS/008863354", (4.786863327026367, 51.262774197393820)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863008", (4.649276733398437, 51.345839804352885)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863009", (4.989852905273437, 51.223657764702750)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863010", (4.955863952636719, 51.325462944331300)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863011", (4.830207824707031, 51.373280620643370)));
            db.Add(new Stop("http://irail.be/stations/NMBS/008863012", (5.538825988769531, 51.177621156752494)));

            db.PostProcess(14);
            var stop = db.FindClosest((4.78686332702636, 51.26277419739382), 50000);
            Assert.NotNull(stop);
            Assert.Equal(db.Get(id1), stop);
        }


        [Fact]
        public void FindClosest_CachedReader_ExpectsNo1()
        {
            var stopsdb = new SimpleStopsDb(0);
            var db = stopsdb;
            var allStops = new List<Stop>
            {
                new Stop("http://irail.be/stations/NMBS/008863354", (4.786863327026367, 51.262774197393820)),
                new Stop("http://irail.be/stations/NMBS/008863008", (4.649276733398437, 51.345839804352885)),
                new Stop("http://irail.be/stations/NMBS/008863009", (4.989852905273437, 51.223657764702750)),
                new Stop("http://irail.be/stations/NMBS/008863010", (4.955863952636719, 51.325462944331300)),
                new Stop("http://irail.be/stations/NMBS/008863011", (4.830207824707031, 51.373280620643370)),
                new Stop("http://irail.be/stations/NMBS/008863012", (5.538825988769531, 51.177621156752494))
            };

            foreach (var s in allStops)
            {
                stopsdb.Add(s);
            }

            db.PostProcess(14);
            var reader = stopsdb.UseCache();

            var l0 = (4.78686332702636, 51.26277419739382);
            var minDist = allStops.Select(s => DistanceEstimate.DistanceEstimateInMeter(l0, (s.Longitude, s.Latitude)))
                .Min();
            Assert.True(minDist < 500);
            Assert.NotEmpty(stopsdb.GetInRange(l0, 500));

            var stop = reader.FindClosest(l0, 500);
            Assert.NotNull(stop);
            var stop0 = reader.FindClosest((4.78686332702636, 51.26277419739382), 500);
            Assert.Equal(allStops[0], stop);
            Assert.Equal(allStops[0], stop0);
        }
    }
}