using System;
using Itinero.Transit;
using Itinero.Transit.Tests;
using Reminiscence.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    /// <summary>
    /// Used to test performance of the algorithms. It's not really the correctness that is tested here
    /// </summary>
    public class SpeedTest : SuperTest
    {
        private Profile<TransferStats> Sncb;

        public SpeedTest(ITestOutputHelper output) : base(output)
        {
            Sncb = Belgium.Sncb(new LocalStorage(ResourcesTest.TestPath),
                new Downloader(caching: true));
        }

        [Fact]
        public void TestEas1()
        {
            var nrOfRuns = 10;
            Tic();
            for (int i = 0; i < nrOfRuns; i++)
            {
                Sncb.CalculateEas(TestEas.Poperinge, TestEas.Vielsalm, ResourcesTest.TestMoment(10, 00),
                    ResourcesTest.TestMoment(20, 00));
            }

            var time = Toc();
            Pr($"Needed {time} millsec ({time/nrOfRuns}/case)");
        }

        private Uri cUri(int i)
        {
            return new Uri("http://www.example.com/" + i);
        }

        [Fact]
        public void DictImplementationCompare()
        {
            var totalLengthTarget = 1000 * 1000 * 1;

            var r = new Random();
            Dictionary<string, int> rem = new Dictionary<string, int>();
            System.Collections.Generic.Dictionary<Uri, int>
                syst = new System.Collections.Generic.Dictionary<Uri, int>();


            Pr($"# Challenge 1: Building a dictionary with {totalLengthTarget} elements");

            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                rem[cUri(i).ToString()] = r.Next();
            }


            var remTime = Toc();

            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                syst[cUri(i)] = r.Next();
            }

            var sysTime = Toc();
            var winner = "";
            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");

            Pr($"# Challenge 2: enumeration");

            // Second round: iteration

            var sum = 0;

            Pr("# Challenge 3: classic for iteration");
            sum = 0;
            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                sum += rem[cUri(i).ToString()];
            }

            remTime = Toc();


            sum = 0;
            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                sum += syst[cUri(i)];
            }

            sysTime = Toc();
            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");


            // Still system that's winning
            Assert.True(sysTime < remTime);


            Pr("# Challenge 4: copy constructors ");
            Tic();
            var remCl = new Dictionary<string, int>();
            // new List<int>();// Oh, there is no copy constructor
            for (int i = 0; i < totalLengthTarget; i++)
            {
                remCl.Add(cUri(i).ToString(), rem[cUri(i).ToString()]);
            }

            remTime = Toc();

            Tic();

            var sysCl = new System.Collections.Generic.Dictionary<Uri, int>(syst);
            sysTime = Toc();


            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");


            Pr("# Challenge 5: head removal");

            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                remCl.Remove(cUri(i).ToString());
            }

            remTime = Toc();


            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                sysCl.Remove(cUri(i));
            }

            sysTime = Toc();
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");
        }

        [Fact]
        public void ListImplementationCompare()
        {
            var totalLengthTarget = 1000 * 1000 * 100;

            List<int> reminiscience = new List<int>();
            System.Collections.Generic.List<int> syst = new System.Collections.Generic.List<int>();

            Pr($"# Challenge 1: Building a list with {totalLengthTarget} elements");
            var r = new Random();


            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                reminiscience.Add(r.Next());
            }


            var remTime = Toc();

            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                syst.Add(r.Next());
            }

            var sysTime = Toc();
            var winner = "";
            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");

            Pr($"# Challenge 2: enumeration");

            // Second round: iteration

            var sum = 0;
            Tic();
            foreach (var i in reminiscience)
            {
                sum += i;
            }

            remTime = Toc();

            sum = 0;
            Tic();
            foreach (var i in syst)
            {
                sum += i;
            }

            sysTime = Toc();
            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");


            Pr("# Challenge 3: classic for iteration");
            sum = 0;
            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                sum += reminiscience[i];
            }

            remTime = Toc();


            sum = 0;
            Tic();
            for (int i = 0; i < totalLengthTarget; i++)
            {
                sum += syst[i];
            }

            sysTime = Toc();
            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");


            // Still system that's winning
            Assert.True(sysTime < remTime);


            Pr("# Challenge 4: copy constructors ");
            Tic();
            var remCl = new List<int>(); // Oh, there is no copy constructor
            foreach (var i in reminiscience)
            {
                remCl.Add(i);
            }

            remTime = Toc();

            Tic();

            System.Collections.Generic.List<int> sysCl = new System.Collections.Generic.List<int>(syst);
            sysTime = Toc();


            winner = (sysTime < remTime) ? "System" : "Reminiscience";
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");


            Pr("# Challenge 5: head removal");

            Tic();
            for (int i = 0; i < 10; i++)
            {
                remCl.RemoveAt(0);
            }

            remTime = Toc();


            Tic();
            for (int i = 0; i < 10; i++)
            {
                sysCl.RemoveAt(0);
            }

            sysTime = Toc();
            Log($"{winner} wins. Reminiscience took {remTime} ms, System took {sysTime}ms");
        }
    }
}