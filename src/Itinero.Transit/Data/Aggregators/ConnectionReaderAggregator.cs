using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data.Aggregators
{
    public class ConnectionReaderAggregator : IConnectionReader
    {
        private readonly IEnumerable<IConnectionReader> _readers;
        private IConnection _currentConnection;


        public static IConnectionReader CreateFrom(IEnumerable<IConnectionReader> readers)
        {
            if (!readers.Any())
            {
                throw new ArgumentException("At least one IConnectionReader is needed to aggregate them");
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (readers.Count() == 1)
            {
                return readers.First();
            }

            return new ConnectionReaderAggregator(readers);
        }

        private ConnectionReaderAggregator(IEnumerable<IConnectionReader> readers)
        {
            _readers = readers;
        }


        public bool MoveTo(uint dbId, uint connectionId)
        {
            foreach (var reader in _readers)
            {
                // ReSharper disable once InvertIf
                if (reader.MoveTo(dbId, connectionId))
                {
                    _currentConnection = reader;
                    return true;
                }
            }

            return false;
        }


        public string GlobalId => _currentConnection.GlobalId;
        public ulong ArrivalTime => _currentConnection.ArrivalTime;

        public ulong DepartureTime => _currentConnection.DepartureTime;

        public ushort TravelTime => _currentConnection.TravelTime;

        public ushort ArrivalDelay => _currentConnection.ArrivalDelay;

        public ushort DepartureDelay => _currentConnection.DepartureDelay;

        public ushort Mode => _currentConnection.Mode;

        public TripId TripId => _currentConnection.TripId;

        public LocationId DepartureStop => _currentConnection.DepartureStop;

        public LocationId ArrivalStop => _currentConnection.ArrivalStop;

        public uint Id => _currentConnection.Id;

    }
}