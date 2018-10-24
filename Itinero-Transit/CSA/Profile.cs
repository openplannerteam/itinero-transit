using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A profile represents the preferences of the traveller.
    /// Which PT-operators does he want to take? Which doesn't he?
    /// How fast does he walk? All these are stored here
    /// </summary>
    public class Profile<T> where T : IJourneyStats<T>
    {
        public readonly IConnectionsProvider ConnectionsProvider;
        public readonly ILocationProvider LocationProvider;
        public readonly IFootpathTransferGenerator FootpathTransferGenerator;

        public readonly T StatsFactory;
        public readonly StatsComparator<T> ProfileCompare, ParetoCompare;

        public Profile(IConnectionsProvider connectionsProvider,
            ILocationProvider locationProvider,
            IFootpathTransferGenerator footpathTransferGenerator,
            T statsFactory,
            StatsComparator<T> profileCompare,
            StatsComparator<T> paretoCompare)
        {
            ConnectionsProvider = connectionsProvider;
            LocationProvider = locationProvider;
            FootpathTransferGenerator = footpathTransferGenerator;
            StatsFactory = statsFactory;
            ProfileCompare = profileCompare;
            ParetoCompare = paretoCompare;
        }


        public Journey<T> CalculateEas(Uri departure, Uri arrival, DateTime departureTime, DateTime endTime)
        {
            var eas = new EarliestConnectionScan<T>(departure, arrival, departureTime, endTime, this);
            return eas.CalculateJourney();
        }


        public IEnumerable<IContinuousConnection> WalkToClosebyStops(DateTime departureTime, Location from, int radius)
        {
            var close = LocationProvider.GetLocationsCloseTo(from.Lat, from.Lon, radius);
            var result = new HashSet<IContinuousConnection>();
            foreach (var stop in close)
            {
                var transfer = FootpathTransferGenerator.GenerateFootPaths(departureTime,
                    from, LocationProvider.GetCoordinateFor(stop));
                result.Add(transfer);
            }

            return result;
        }


        public IEnumerable<IContinuousConnection> WalkFromClosebyStops(DateTime arrivalTime, Location to, int radius)
        {
            var close = LocationProvider.GetLocationsCloseTo(to.Lat, to.Lon, radius);
            var result = new HashSet<IContinuousConnection>();
            foreach (var stop in close)
            {
                var transfer = FootpathTransferGenerator.GenerateFootPaths(arrivalTime,
                    LocationProvider.GetCoordinateFor(stop), to);
                var diff = transfer.ArrivalTime() - transfer.DepartureTime();
                transfer.MoveTime(-diff.TotalSeconds);
                result.Add(transfer);
            }

            return result;
        }

        /// <summary>
        /// Given two connections (e.g. within the same station; or to a bus station which is close by),
        /// calculates an object representing the transfer (e.g. walking from platform 2 to platform 5; or walking 250 meters)
        /// </summary>
        /// <param name="from">The connection that the newly calculated connection continues on</param>
        /// <param name="to">The connection that should be taken after the returned connection</param>
        /// <returns>A connection representing the transfer. Returns null if no transfer is possible (e.g. to little time)</returns>
        public IConnection CalculateInterConnection(IConnection from, IConnection to)
        {
            var footpath = FootpathTransferGenerator.GenerateFootPaths(
                from.ArrivalTime(),
                LocationProvider.GetCoordinateFor(from.ArrivalLocation()),
                LocationProvider.GetCoordinateFor(to.DepartureLocation()));

            if (footpath.ArrivalTime() > to.DepartureTime())
            {
                // we can't make it in time to the connection where we are supposed to go
                return null;
            }

            return footpath;
        }
    }
}