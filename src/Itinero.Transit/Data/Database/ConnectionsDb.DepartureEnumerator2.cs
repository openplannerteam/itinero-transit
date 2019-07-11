namespace Itinero.Transit.Data
{
    public partial class ConnectionsDb
    {
        /// <summary>
        /// The departureEnumerator uses the departureWindowIndexes to crawl through the connectionsDB
        ///
        /// The object itself is pretty stateless, all the relevant state is saved in the 'DepartureTimeIndex'.
        ///
        /// 
        /// </summary>
        public class DepartureEnumerator : IConnectionEnumerator
        {
            private readonly ConnectionsDb _connectionsDb;
            private readonly ConnectionsDbReader _reader;


            /// <summary>
            /// The current DateTime
            /// This implies which window we want to use for the IndexInWindow
            /// </summary>
            public ulong CurrentDateTime { get; private set; }

            private uint _connectionInternalId = uint.MaxValue;

            private uint _indexInWindow;

            public DepartureEnumerator(
                ConnectionsDb connectionsDb,
                ConnectionsDbReader reader)
            {
                _connectionsDb = connectionsDb;
                _reader = reader;
                _indexInWindow = 0;
            }

            public void MoveTo(ulong dateTime)
            {
                // An initial data pointer: we are given the right window at index 0
                // Next will figure out if that data actually exist and run through the windows until it finds an actual connection
                CurrentDateTime = dateTime;
                _indexInWindow = uint.MaxValue;
                _connectionInternalId = uint.MaxValue;
            }


            /// <summary>
            /// Gives a DTI where the CurrentDateTime points to the window just after the current window
            /// </summary>
            /// <returns></returns>
            private void NextWindow()
            {
                // We increase the dateTime so that it is exactly the start of the next window

                CurrentDateTime =
                    ((CurrentDateTime / _connectionsDb._windowSizeInSeconds) + 1) *
                    _connectionsDb._windowSizeInSeconds;
                _indexInWindow = 0;
            }


            /// <summary>
            /// Determines the next DepartureTimeIndex. If it is found, it will be written into 'current'
            /// If there is no next value because the connectionDb is depleted, false will be returned.
            ///
            /// This method will automatically skip empty windows.
            ///
            /// </summary>
            /// <returns></returns>
            public bool HasNext()
            {
                if (_indexInWindow == uint.MaxValue)
                {
                    // Needs some initialization
                    _indexInWindow = 0;
                }
                // ALL RIGHT FOLKS
                // Time to figure things out!
                // We need the connection in the window for the given datetime, at the given index
                // IF it does not exist, we go to the next existing connection

                // For starters, what is the wanted window and does it exist?
                var window = _connectionsDb.WindowFor(CurrentDateTime);
                var windowPointer = _connectionsDb._departureWindowPointers[window * 2 + 0];

                if (windowPointer == _noData)
                {
                    // Nope, that window is not there!

                    // Either this window just happens to be empty
                    // Or we are at the end of our connections database

                    if (CurrentDateTime > _connectionsDb.LatestDate)
                    {
                        // Yep, the database is depleted
                        return false;
                    }

                    // There might be a next window available
                    NextWindow();
                    return HasNext();
                }


                ulong depTime;
                do
                {
                    // Ok, so we at least have te right window.
                    // Lets see if we can retrieve the connection itself
                    // For that, we should check if the index is within the window size
                    var windowSize = _connectionsDb._departureWindowPointers[window * 2 + 1];
                    if (_indexInWindow >= windowSize)
                    {
                        // Ahh, the good old 'IndexOutOfBounds'
                        // In other words, this window is simply depleted
                        // We attempt to use the next window
                        NextWindow();
                        return HasNext();
                    }


                    // Ok, so we have the right window and the connection exists! Hooray!
                    _connectionInternalId = _connectionsDb._departurePointers[windowPointer + _indexInWindow];

                    // We update the DTI
                    _indexInWindow++;

                    // and get the departure time of the index because...
                    depTime = _connectionsDb.GetConnectionDeparture(_connectionInternalId);

                    // ... the current connection could fall _too soon_
                    // Either just because of the specified dateTime
                    // or because a window has connections from multiple days
                    // If that happens, we just restart everything:
                } while (depTime < CurrentDateTime);

                // If we end up here, the desired connection exists and its departure time falls after the specified time
                // current.WindowIndex points to the next needed element in the window
                // And current.ConnectionInternalId is set
                // So, we are pretty much done

                // Only thing that rests us is top update the current.DepartureTime, so that the caller knows when this connection is leaving
                // This is used by the aggregator
                CurrentDateTime = depTime;
                return true;
            }

            private void PreviousWindow()
            {
                // We decrease the dateTime so that it is exactly the end of the previous window

                // First, put it at the start of the current window
                CurrentDateTime =
                    (CurrentDateTime / _connectionsDb._windowSizeInSeconds) *
                    _connectionsDb._windowSizeInSeconds;
                // And decrease by one
                CurrentDateTime--;

                // And we should point to its last element

                var window = _connectionsDb.WindowFor(CurrentDateTime);
                _indexInWindow = _connectionsDb._departureWindowPointers[window * 2 + 1];
            }

            public bool HasPrevious()
            {
                if (CurrentDateTime == 0)
                {
                    return false;
                }

                if (_indexInWindow == uint.MaxValue)
                {
                    // Needs some initialization
                    var w = _connectionsDb.WindowFor(CurrentDateTime);
                    var size = _connectionsDb._departureWindowPointers[w * 2 + 1];

                    _indexInWindow = size;
                }

                // ALL RIGHT FOLKS
                // Time to figure things out!
                // We need the connection in the window for the given datetime, at the given index
                // IF it does not exist, we go to the next existing connection

                // For starters, what is the wanted window and does it exist?
                var window = _connectionsDb.WindowFor(CurrentDateTime);
                var windowPointer = _connectionsDb._departureWindowPointers[window * 2 + 0];

                if (windowPointer == _noData)
                {
                    // Nope, that window is not there!

                    // Either this window just happens to be empty
                    // Or we are at the end of our connections database

                    if (CurrentDateTime < _connectionsDb.EarliestDate)
                    {
                        // Yep, the database is depleted
                        return false;
                    }

                    // There might be a next window available
                    PreviousWindow();
                    return HasPrevious();
                }


                ulong depTime;
                do
                {
                    // Ok, so we at least have te right window.
                    // Lets see if we can retrieve the connection itself
                    // For that, we should check if the index is within the window size
                    if (_indexInWindow < 1)
                    {
                        // Ahh, the good old 'IndexOutOfBounds'
                        // In other words, this window is simply depleted
                        // We attempt to use the next window
                        PreviousWindow();
                        return HasPrevious();
                    }


                    // Ok, so we have the right window and the connection exists! Hooray!
                    _connectionInternalId = _connectionsDb._departurePointers[windowPointer + _indexInWindow - 1];

                    // We update the DTI
                    _indexInWindow--;

                    // and get the departure time of the index because...
                    depTime = _connectionsDb.GetConnectionDeparture(_connectionInternalId);

                    // ... the current connection could fall _too late_
                    // Either just because of the specified dateTime
                    // or because a window has connections from multiple days
                    // If that happens, we just restart everything:
                } while (depTime > CurrentDateTime);

                // If we end up here, the desired connection exists and its departure time falls before the specified time
                // current.WindowIndex points to the next needed element in the window
                // And current.ConnectionInternalId is set
                // So, we are pretty much done

                // Only thing that rests us is top update the current.DepartureTime, so that the caller knows when this connection is leaving
                // This is used by the aggregator
                CurrentDateTime = depTime;
                return true;
            }

            public bool Current(Connection toWrite)
            {
                return _reader.Get(new ConnectionId(_connectionsDb.DatabaseId, _connectionInternalId), toWrite);
            }
        }
    }
}