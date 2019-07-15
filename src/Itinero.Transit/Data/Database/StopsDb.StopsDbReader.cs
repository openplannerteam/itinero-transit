using System.Collections.Generic;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Tiles;

namespace Itinero.Transit.Data
{
    public partial class StopsDb : IDatabaseReader<StopId, Stop>
    {
        
        
        
        public bool Get(StopId id, Stop objectToWrite)
        {
            
            
            
            
            
            throw new System.NotImplementedException();
        }

        public bool Get(string globalId, Stop objectToWrite)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<uint> DatabaseIds { get; }
        public class StopsDbReader : IStopsReader
        {
            private readonly TiledLocationIndex.Enumerator _locationEnumerator;

            internal StopsDbReader(StopsDb stopsDb)
            {
                StopsDb = stopsDb;
                _locationEnumerator = StopsDb._stopLocations.GetEnumerator();
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            // ReSharper disable once UnusedMember.Global
            public void Reset()
            {
                _locationEnumerator.Reset();
            }

            /// <summary>
            /// Enumerates all stops in the given bounding box.
            /// </summary>
            /// <param name="box">The box to enumerate in.</param>
            /// <returns>An enumerator with all the stops.</returns>
            public IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box)
            {
                var rangeStops = new TileRangeStopEnumerable(StopsDb, box);
                using (var rangeStopsEnumerator = rangeStops.GetEnumerator())
                {
                    while (rangeStopsEnumerator.MoveNext())
                    {
                        var location = rangeStopsEnumerator.Current;

                        if (location == null)
                        {
                            continue;
                        }

                        if (box.minLat > location.Latitude ||
                            box.minLon > location.Longitude ||
                            box.maxLat < location.Latitude ||
                            box.maxLon < location.Longitude)
                        {
                            continue;
                        }

                        yield return rangeStopsEnumerator.Current;
                    }
                }
            }

            /// <summary>
            /// Moves this enumerator to the given stop.
            /// </summary>
            /// <param name="localTileId">The local tile id.</param>
            /// <param name="localId">The local id.</param>
            /// <returns>True if there is more data.</returns>
            private bool MoveTo(uint localTileId, uint localId)
            {
                return _locationEnumerator.MoveTo(localTileId, localId);
            }

            /// <summary>
            /// Moves this enumerator to the given stop.
            /// </summary>
            /// <param name="stop">The stop.</param>
            /// <returns>True if there is more data.</returns>
            public bool MoveTo(StopId stop)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (StopsDb.DatabaseId != stop.DatabaseId)
                {
                    return false;
                }

                return MoveTo(stop.LocalTileId, stop.LocalId);
            }

            /// <summary>
            /// Moves this enumerator to the stop with the given global id.
            /// </summary>
            /// <param name="globalId">The global id.</param>
            /// <returns>True if the stop was found and there is data.</returns>
            public bool MoveTo(string globalId)
            {
                var hash = StopsDb.Hash(globalId);
                var pointer = StopsDb._stopIdPointersPerHash[hash];
                while (pointer != _noData)
                {
                    var localTileId = StopsDb._stopIdLinkedList[pointer + 0];
                    var localId = StopsDb._stopIdLinkedList[pointer + 1];

                    if (MoveTo(localTileId, localId))
                    {
                        var potentialMatch = GlobalId;
                        if (potentialMatch == globalId)
                        {
                            return true;
                        }
                    }

                    pointer = StopsDb._stopIdLinkedList[pointer + 2];
                }

                return false;
            }

            public HashSet<uint> DatabaseIndexes()
            {
                return new HashSet<uint> {StopsDb.DatabaseId};
            }

            /// <summary>
            /// Moves to the next stop.
            /// </summary>
            /// <returns>True if there is more data.</returns>
            public bool MoveNext()
            {
                return _locationEnumerator.MoveNext();
            }

            /// <inheritdoc />
            public string GlobalId => StopsDb._stopIds[_locationEnumerator.DataPointer];

            /// <inheritdoc />
            public StopId Id =>
                new StopId(StopsDb.DatabaseId, _locationEnumerator.TileId, _locationEnumerator.LocalId);

            /// <inheritdoc />
            public double Latitude => _locationEnumerator.Latitude;

            /// <inheritdoc />
            public double Longitude => _locationEnumerator.Longitude;

            /// <summary>
            /// Gets the attributes.
            /// </summary>
            public IAttributeCollection Attributes =>
                StopsDb._attributes.Get(StopsDb._stopAttributeIds[_locationEnumerator.DataPointer]);

            public StopsDb StopsDb { get; }
        }

      
    }
}