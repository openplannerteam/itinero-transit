using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

// ReSharper disable StaticMemberInGenericType

// ReSharper disable BuiltInTypeReferenceStyle

namespace Itinero.Transit.Journey
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    ///
    /// Normally, a journey is constructed with the start location hidden the deepest in the data structure.
    /// The Time is mostly the arrival time.
    ///
    /// The above properties are reversed in the CPS algorithm. The last step of that algorithm is to reverse the journeys,
    /// so that users of the lib get a uniform experience
    ///
    /// </summary>
    public class Journey<T>
        where T : IJourneyMetric<T>
    {
        public static readonly Journey<T> InfiniteJourney = new Journey<T>();

        public static readonly Journey<T> NegativeInfiniteJourney
            = new Journey<T>(ulong.MinValue);

        /// <summary>
        /// The first link of the journey. Can be useful when in need of the real departure time
        /// </summary>
        public readonly Journey<T> Root;


        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public readonly Journey<T> PreviousLink;

        /// <summary>
        /// Sometimes, we encounter two subjourneys which are equally optimal.
        /// Instead of duplicating them across the graph, we have this special journey part which gives an alternative version split
        /// </summary>
        public readonly Journey<T> AlternativePreviousLink;

        /// <summary>
        /// Indicates that this journeyPart is not a simple PT-connection,
        /// but rather something as a walk, transfer, ...
        /// </summary>
        public readonly bool SpecialConnection;

        /// <summary>
        /// The connection id, taken in this last part of this journey
        /// We resort to magic for special connections (if Special Connection is set), such as walks between stops
        ///
        /// 1 This is the Genesis Connection
        /// 2: This is a Transfer (within the same station)
        /// 3: This is a Walk, from the previous journey collection to here.
        ///         Note that the actual route is _not_ saved as not to use too much memory
        /// 
        /// 
        /// </summary>
        public readonly ConnectionId Connection;

        /// <summary>
        /// Constant indicating that the journey starts here
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly ConnectionId GENESIS = new ConnectionId(uint.MaxValue, 1);

        /// <summary>
        /// Constant indicating that the traveller is in some 'other mode', e.g. transfering, waiting or walking between stops
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly ConnectionId OTHERMODE = new ConnectionId(uint.MaxValue, 2);


        /// <summary>
        /// Indicates that this journey represents a choice
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly ConnectionId JOINED_JOURNEYS = new ConnectionId(uint.MaxValue, 3);

        /// <summary>
        /// Keep track of Location.
        /// In normal circumstances, this is the location where the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure location.
        /// </summary>
        public readonly StopId Location;

        /// <summary>
        /// Keep track of Time.
        /// In normal circumstances (forward built journeys), this is the time when the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure time.
        /// </summary>
        public readonly ulong Time;

        /// <summary>
        /// The trip id of the connection
        /// This is used as a freeform debugging tag in the Genesis Element
        /// </summary>
        public readonly TripId TripId;

        public static readonly TripId EarliestArrivalScanJourney = new TripId(1, 1);
        public static readonly TripId LatestArrivalScanJourney = new TripId(2, 2);
        public static readonly TripId ProfiledScanJourney = new TripId(3, 3);

        /// <summary>
        /// A metric about the journey up till this point
        /// </summary>
        public readonly T Metric;

        /// <summary>
        /// Hashcode is calculated at the start for quick comparisons
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// Infinity constructor
        /// This is the constructor which creates a journey representing an infinite journey.
        /// There is a singleton available: Journey.InfiniteJourney.
        /// This object is used when needing a dummy object to compare to, e.g. as journey to locations that can't be reached
        /// </summary>
        private Journey(ulong time = ulong.MaxValue)
        {
            Root = this;
            PreviousLink = this;
            Connection = ConnectionId.Invalid;
            Location = StopId.Invalid;
            Time = time;
            SpecialConnection = true;
            TripId = new TripId(uint.MaxValue, uint.MaxValue);
            _hashCode = CalculateHashCode();
        }

        /// <summary>
        /// All-exposing private constructor. All values are read-only
        /// 
        /// Note that the metric will not be saved, but be used as constructor with 'metrics.add(this)' is
        /// </summary>
        /// <param name="root"></param>
        /// <param name="previousLink"></param>
        /// <param name="specialLink"></param>
        /// <param name="connection"></param>
        /// <param name="location"></param>
        /// <param name="time"></param>
        /// <param name="tripId"></param>
        /// <param name="metric"></param>
        private Journey(Journey<T> root,
            Journey<T> previousLink, bool specialLink, ConnectionId connection,
            StopId location, ulong time, TripId tripId, T metric)
        {
            Root = root;
            SpecialConnection = specialLink;
            PreviousLink = previousLink;
            Connection = connection;
            Location = location;
            Time = time;
            TripId = tripId;
            Metric = metric.Add(this);
            _hashCode = CalculateHashCode();
        }

        /// <inheritdoc />
        /// <summary>
        /// Genesis constructor.
        /// This constructor creates a root journey
        /// </summary>
        public Journey(StopId location, ulong departureTime, T initialMetric)
            : this(location, departureTime, initialMetric, new TripId(uint.MaxValue, uint.MaxValue))
        {
        }

        /// <summary>
        /// Genesis constructor.
        /// This constructor creates a root journey
        /// </summary>
        public Journey(StopId location, ulong departureTime, T initialMetric,
            TripId debuggingFreeformTag)
        {
            Root = this;
            PreviousLink = null;
            Connection = GENESIS;
            SpecialConnection = true;
            Location = location;
            Time = departureTime;
            Metric = initialMetric;
            TripId = debuggingFreeformTag;
            _hashCode = CalculateHashCode();
        }


        public Journey(Journey<T> optionA, Journey<T> optionB)
        {
            if (optionA == null)
            {
                throw new ArgumentNullException("optionA");
            }

            if (optionB == null)
            {
                throw new ArgumentNullException("optionB");
            }

            AlternativePreviousLink = optionB;
            // Option A is seen as the 'true' predecessor'
            Root = optionA.Root;
            PreviousLink = optionA;
            Connection = JOINED_JOURNEYS;
            SpecialConnection = true;
            Location = optionA.Location;
            Time = optionA.Time;
            Metric = optionA.Metric;
            TripId = optionA.Root.TripId;
            _hashCode = optionA._hashCode + optionB._hashCode;
        }

        /// <summary>
        /// Chaining constructor
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        [Pure]
        internal Journey<T> Chain(ConnectionId connection, ulong time, StopId location,
            TripId tripId)
        {
            return new Journey<T>(
                Root, this, false, connection, location, time, tripId, Metric);
        }

        [Pure]
        public Journey<T> ChainForward(Connection c)
        {
            // ReSharper disable once InvertIf
            if (SpecialConnection && Equals(Connection, GENESIS))
            {
                // We do something special here:
                // We move the genesis to the departure time of the connection
                // This is used by EAS to have a correcter departure time
                var newGenesis = new Journey<T>(Location, c.DepartureTime, Metric.Zero(), TripId);
                return newGenesis.Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
            }

            return Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
        }

        [Pure]
        public Journey<T> ChainBackward(Connection c)
        {
            // ReSharper disable once InvertIf
            if (SpecialConnection && Equals(Connection, GENESIS))
            {
                // We do something special here:
                // We move the genesis to the departure time of the connection
                var newGenesis = new Journey<T>(Location, c.ArrivalTime, Metric.Zero(), TripId);
                return newGenesis.Chain(c.Id, c.DepartureTime, c.DepartureStop, c.TripId);
            }

            return Chain(c.Id, c.DepartureTime, c.DepartureStop, c.TripId);
        }


        /// <summary>
        /// Chaining constructorChain
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        [Pure]
        public Journey<T> ChainSpecial(ConnectionId specialCode, ulong time,
            StopId location, TripId tripId)
        {
            return new Journey<T>(
                Root, this, true, specialCode, location, time, tripId, Metric);
        }

        /// <summary>
        /// Extends this journey with the given connection. If there is waiting time between this journey and the next,
        /// a 'Transfer' link is included.
        /// Transfer links _should not_ be used to calculate the number of transfers, the differences in trip-ids should be used for this! 
        /// </summary>
        [Pure]
        public Journey<T> Transfer(ulong departureTime)
        {
            // Creating the transfer
            return new Journey<T>(
                // ReSharper disable once ArrangeThisQualifier
                Root, this, true, OTHERMODE, this.Location, departureTime, new TripId(uint.MaxValue, uint.MaxValue),
                Metric);
        }

        /// <summary>
        /// Chains an 'othermode' link to this journey, using a forward (with time) logic.
        /// </summary>
        [Pure]
        public Journey<T> ChainForwardWith(IStopsReader reader, IOtherModeGenerator otherModeGenerator,
            StopId otherLocation)
        {
            var time = otherModeGenerator.TimeBetween(reader, Location, otherLocation);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (uint.MaxValue == time)
            {
                return null;
            }

            return ChainSpecial(OTHERMODE, Time + time, otherLocation, new TripId(otherModeGenerator));
        }

        /// <summary>
        /// Chains an 'othermode' link to this journey, using a backward (against time) logic.
        /// </summary>
        [Pure]
        public Journey<T> ChainBackwardWith(IStopsReader reader, IOtherModeGenerator otherModeGenerator,
            StopId otherLocation)
        {
            var time = otherModeGenerator.TimeBetween(reader, Location, otherLocation);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (uint.MaxValue == time)
            {
                return null;
            }

            // Pretty much the only difference is the '-' here instead of a '+' with the forward method
            return ChainSpecial(OTHERMODE, Time - time, otherLocation, new TripId(otherModeGenerator));
        }


        /// <summary>
        /// Returns the trip id of the most recent connection which has a valid trip.
        /// </summary>
        [Pure]
        public TripId? LastTripId()
        {
            return SpecialConnection ? PreviousLink?.LastTripId() : TripId;
        }

        /// <summary>
        /// Departure time of this journey part
        /// </summary>
        /// <returns></returns>
        [Pure]
        public ulong DepartureTime()
        {
            if (SpecialConnection && Equals(Connection, GENESIS))
            {
                return Time;
            }

            return PreviousLink.Time;
        }

        /// <summary>
        /// Arrival time of this journey part
        /// </summary>
        /// <returns></returns>
        [Pure]
        public ulong ArrivalTime()
        {
            return Time;
        }


        /// <summary>
        /// Given a journey and a reversed journey, append the reversed journey to the journey
        /// </summary>
        [Pure]
        public Journey<T> Append(Journey<T> restingJourney)
        {
            var j = this;
            while (restingJourney != null &&
                   (!restingJourney.SpecialConnection ||
                    !Equals(restingJourney.Connection, GENESIS)))
            {
                // Resting journey is backwards - so restingJourney is departure, restingJourney.PreviousLink the arrival time
                var timeDiff =
                    (long) restingJourney.Time -
                    (long) restingJourney.PreviousLink.Time; // Cast to long to allow negative values
                j = new Journey<T>(
                    j.Root,
                    j,
                    restingJourney.SpecialConnection,
                    restingJourney.Connection,
                    restingJourney.PreviousLink.Location,
                    j.Time + (ulong) timeDiff,
                    restingJourney.TripId,
                    j.Metric
                );
                restingJourney = restingJourney.PreviousLink;
            }

            return j;
        }

        [Pure]
        internal string ToString(TransitDb.TransitDbSnapShot snapshot)
        {
            return ToString(15, snapshot.StopsDb.GetReader());
        }

        [Pure]
        public string ToString(WithTime<T> withTime)
        {
            return ToString(15, withTime.StopsReader);
        }


        public string ToString(WithProfile<TransferMetric> withProfile)
        {
            return ToString(15, withProfile.StopsReader);
        }

        [Pure]
        public string ToString(uint truncateAt, IStopsReader stops = null, IDatabaseReader<ConnectionId, Connection> connection = null)
        {
            if (Equals(InfiniteJourney))
            {
                return "Journey impossible/infinite";
            }

            var asList = this.ToList();


            var snippets = asList.Select(j => j.PartToString(stops, connection)).ToList();

            if (snippets.Count > truncateAt)
            {
                var allSnippets = snippets;
                var partL = (int) truncateAt / 2;
                snippets = allSnippets.GetRange(0, partL);
                snippets.Add("... journey truncated ...");
                snippets.AddRange(
                    allSnippets.GetRange(
                        allSnippets.Count - partL, partL));
            }

            var totals = $" ({Metric})";


            return string.Join("\n", snippets) + totals;
        }

        [Pure]
        private string PartToString(IStopsReader stops,
            IDatabaseReader<ConnectionId, Connection> connectionReader)
        {
            var location = Location.ToString();
            var dbOperator = uint.MaxValue;

            if (stops != null)
            {
                stops.MoveTo(Location);
                location = stops.GlobalId + " " + stops.Attributes;
                dbOperator = stops.Id.DatabaseId;
            }

            var md = "";
            if (connectionReader != null)
            {
                var connection = connectionReader.Get(Connection);
                switch (connection.Mode % 4)
                {
                    case 1:
                        md = "; Only getting on";
                        break;
                    case 2:
                        md = "; Only getting off";
                        break;
                    case 3:
                        md = "; No getting off/getting on";
                        break;
                    default:
                        md = "; normal connection";
                        break;
                }

                if ((connection.Mode & 4) == 4)
                {
                    md += "; CANCELLED";
                }
            }

            if (SpecialConnection)
            {
                return SpecialPartToString(location);
            }

            return
                $"Connection {Connection} to {location}, arriving at {Time.FromUnixTime():s}; operator is {dbOperator}{md}";
        }

        private string SpecialPartToString(string location)
        {
            if (Connection.Equals(GENESIS))
            {
                var freeForm = ", debugging free form tag is ";

                if (TripId.Equals(EarliestArrivalScanJourney))
                {
                    freeForm += "EAS";
                }
                else if (TripId.Equals(LatestArrivalScanJourney))
                {
                    freeForm += "LAS";
                }
                else if (TripId.Equals(ProfiledScanJourney))
                {
                    freeForm += "PCS";
                }
                else
                {
                    freeForm = TripId.ToString();
                }

                return
                    $"Genesis at {location}, time is {Time.FromUnixTime():s}{freeForm}";
            }

            if (Connection.Equals(OTHERMODE))
            {
                return
                    $"Transfer/Wait/Walk for {Math.Abs((long) Time - (long) PreviousLink.Time)} seconds till {Time.FromUnixTime():s} in/to {location}";
            }

            if (Connection.Equals(JOINED_JOURNEYS))
            {
                return
                    $"Choose a journey: there is a equivalent journey available. Continuing print via one arbitrary option";
            }

            throw new ArgumentException($"Unknown Special Connection code {Connection}");
        }


        [Pure]
        private bool Equals(Journey<T> other)
        {
            return _hashCode == other._hashCode;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Journey<T>) obj);
        }

        [Pure]
        public override int GetHashCode()
        {
            return _hashCode;
        }

        [Pure]
        private int CalculateHashCode()
        {
            unchecked
            {
                var hashCode = SpecialConnection.GetHashCode();
                hashCode = (hashCode * 397) ^ (PreviousLink?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Connection.GetHashCode();
                hashCode = (hashCode * 397) ^ Location.GetHashCode();
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ TripId.GetHashCode();
                return hashCode;
            }
        }
    }
}