//using System;
//using JsonLD.Core;
//using Xunit;
//using Xunit.Abstractions;
//
//// ReSharper disable FieldCanBeMadeReadOnly.Global
//// ReSharper disable UnusedMember.Global
//// ReSharper disable PossibleMultipleEnumeration
//
//namespace Itinero.Transit.Tests
//{
//    public class TestCatalog
//    {
//        private readonly ITestOutputHelper _output;
//
//        public TestCatalog(ITestOutputHelper output)
//        {
//            _output = output;
//        }
//
//
//
//        // ReSharper disable once UnusedMember.Local
//        private void Log(string s)
//        {
//            _output.WriteLine(s);
//        }
//
//
//        [Fact]
//        public void TestDeLijnCatalog()
//        {
//            
//            var lcc = new LinkedConnectionsCatalog(new Uri("http://openplanner.ilabt.imec.be/catalog"));
//            lcc.Download(new JsonLdProcessor(new HttpDocumentDownloader(), new Uri("http://openplanner.ilabt.imec.be/") ));
//            
//            Assert.Equal(5, lcc.Catalogae.Count);
//        }
//        
//        
//    }
//}