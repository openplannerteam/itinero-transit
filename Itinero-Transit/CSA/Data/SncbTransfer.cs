using System;
using Itinero_Transit.CSA;

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// A 'connection' representing a transfer between two SNCB-trains
    /// </summary>
    public class SncbTransfer : IConnection
    {
        private readonly Uri _location; // TODO should be updated to an Uri indicating the platforms
        private readonly DateTime _departureTime, _arrivalTime;

        public SncbTransfer(Uri location, DateTime arrivalTime, DateTime departureTime)
        {
            _location = location;
            _arrivalTime = arrivalTime;
            _departureTime = departureTime;
        }

        public Uri Operator()
        {
            return new Uri("https://www.belgiantrain.be/");
        }

        public string Mode()
        {
            return "Transfer";
        }

        public Uri Id()
        {
            return _location;
        }

        public Uri Trip()
        {
            return null;
        }

        public Uri Route()
        {
            return null;
        }

        public Uri DepartureLocation()
        {
            return _location;
        }

        public Uri ArrivalLocation()
        {
            return _location;
        }

        public DateTime ArrivalTime()
        {
            return _arrivalTime;
        }

        public DateTime DepartureTime()
        {
            return _departureTime;
        }

        public bool Continuous()
        {
            return true;
        }
    }
}