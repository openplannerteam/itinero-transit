using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface IWriter : IGlobalId
    {
        TransitDbSnapShot GetSnapshot();

        IStopsDb Stops { get; }
        IConnectionsDb Connections { get; }
        ITripsDb Trips { get; }

        void SetAttribute(string key, string value);
        void SetGlobalId(string key);

        StopId AddOrUpdateStop(Stop stop);

        ConnectionId AddOrUpdateConnection(Connection connection);

        TripId AddOrUpdateTrip(Trip trip);

        TripId AddOrUpdateTrip(string globalId);
    }

    public static class WriterExtensions
    {
        public static void CopyAttributesFrom(this IWriter writer, IGlobalId propertiesToCopy)
        {
            writer.SetGlobalId(propertiesToCopy.GlobalId);
            foreach (var kv in propertiesToCopy.Attributes)
            {
                writer.SetAttribute(kv.Key, kv.Value);
                
            }
        }


        public static void CopyDataFrom(this IWriter writer, TransitDbSnapShot snapShot)
        {
            writer.CopyAttributesFrom(snapShot);

            foreach (var stop in snapShot.Stops)
            {
                writer.AddOrUpdateStop(stop);
            }

            foreach (var trip in snapShot.Trips)
            {
                writer.AddOrUpdateTrip(trip);
            }

            foreach (var connection in snapShot.Connections)
            {
                writer.AddOrUpdateConnection(connection);
            }
        }
    }
}