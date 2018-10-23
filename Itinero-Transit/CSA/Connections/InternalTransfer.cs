using System;
using Itinero_Transit.CSA.LocationProviders;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// A 'connection' representing a transfer between two platforms, without leaving the station.
    /// They give a fixed transfer time. Normally, the locations of both connections should be the same.
    /// </summary>
    [Serializable()]
    public class InternalTransfer : IContinuousConnection
    {
        private readonly Uri _location;
        private readonly DateTime _departureTime, _arrivalTime;

        public InternalTransfer(Uri location, DateTime departureTime, DateTime arrivalTime)
        {
            _location = location;
            _arrivalTime = arrivalTime;
            _departureTime = departureTime;
            if (arrivalTime < departureTime)
            {
                throw new ArgumentException("You are walking to the past; arrivalTime < departuretime");
            }
        }

        public Uri Operator()
        {
            return null;
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

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(ILocationProvider locDecode)
        {
            return $"Transfer in {locDecode.GetNameOf(_location)} {_departureTime} --> {_arrivalTime}";
        }

        public IContinuousConnection MoveTime(int seconds)
        {
            return new InternalTransfer(_location, _departureTime.AddSeconds(seconds),
                _arrivalTime.AddSeconds(seconds));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InternalTransfer tr))
            {
                return false;
            }

            return Equals(tr);
        }

        private bool Equals(InternalTransfer other)
        {
            return Equals(_location, other._location) &&
                   _departureTime.Equals(other._departureTime) &&
                   _arrivalTime.Equals(other._arrivalTime);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_location != null ? _location.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _departureTime.GetHashCode();
                hashCode = (hashCode * 397) ^ _arrivalTime.GetHashCode();
                return hashCode;
            }
        }
    }
}