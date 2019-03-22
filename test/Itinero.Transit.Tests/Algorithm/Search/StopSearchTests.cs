using System.Linq;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;
using Xunit;

// ReSharper disable UnusedVariable

namespace Itinero.Transit.Tests.Algorithm.Search
{
    public class StopSearchTests
    {
        [Fact]
        public void StopsSearch_ShouldEnumerateAllInBBox()
        {
            var db = new StopsDb(0);
            var id1 = db.Add("http://irail.be/stations/NMBS/008863354", 4.786863327026367, 51.262774197393820);
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

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
            var id2 = db.Add("http://irail.be/stations/NMBS/008863008", 4.649276733398437, 51.345839804352885);
            var id3 = db.Add("http://irail.be/stations/NMBS/008863009", 4.989852905273437, 51.223657764702750);
            var id4 = db.Add("http://irail.be/stations/NMBS/008863010", 4.955863952636719, 51.325462944331300);
            var id5 = db.Add("http://irail.be/stations/NMBS/008863011", 4.830207824707031, 51.373280620643370);
            var id6 = db.Add("http://irail.be/stations/NMBS/008863012", 5.538825988769531, 51.177621156752494);

            var stop = db.GetReader().SearchClosest(4.78686332702636, 51.26277419739382);
            Assert.NotNull(stop);
            Assert.Equal(id1, stop.Id);
        }
    }
}