using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Regression
{
    public class DoubleVielsalm : FunctionalTest<bool, bool>
    {
        public const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        public const string Angleur = "http://irail.be/stations/NMBS/008842002";


        protected override bool Execute(bool input)
        {
            //  $idp --read-tdb fixed-test-cases-sncb-2019-05-22.transitdb --select-stops top=50.7 bottom=50.25 left=5.5 right=6
            // -- 
            return input;
        }
    }
}