using System;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;
using Xunit;

namespace Itinero.Transit.Tests.IO.LC.Synchronization
{
    public class SynchronizerTest
    {
        [Fact]
        public void TestLoading()
        {
            var triggered = false;


            void Update(TransitDb.TransitDbWriter wr, DateTime start, DateTime end)
            {
                triggered = true;
                Assert.Equal(TimeSpan.FromSeconds(3), end - start);
            }

            var tdb = new TransitDb();


            var sync = new Synchronizer(tdb, Update,
                new SynchronizedWindow(1, TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(2)));

            Thread.Sleep(1200);
            Assert.True(triggered);
        }


        [Fact]
        public void TestLoading0()
        {
            var triggered5 = 0;
            var triggered10 = 0;

            void Update(TransitDb.TransitDbWriter wr, DateTime start, DateTime end)
            {
                var diff = (int) (end - start).TotalSeconds;
                if (diff == 5)
                {
                    triggered5++;
                }
                else
                {
                    triggered10++;
                }
            }

            var tdb = new TransitDb();


            var sync = new Synchronizer(tdb, Update,
                new SynchronizedWindow(5, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5)),
                new SynchronizedWindow(10, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10)));


            Thread.Sleep(11000);

            Assert.True(2 <= triggered5);
            Assert.True(4 >= triggered5);
            Assert.True(1 <= triggered10);
            Assert.True(3 >= triggered10);
        }
    }
}