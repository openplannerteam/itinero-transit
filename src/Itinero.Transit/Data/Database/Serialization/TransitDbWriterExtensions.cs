using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Serialization
{
    public static class TransitDbWriterExtensions
    {
        public static void ReadFrom(this IWriter writer, string path)
        {
            using (Stream s = File.OpenRead(path))
            {
                writer.ReadFrom(s);
            }
        } 
        
        public static void ReadFrom(this IWriter writer, Stream stream)
        {
            var formatter = new BinaryFormatter();

            writer.SetGlobalId((string) formatter.Deserialize(stream));
            var attributes = (IReadOnlyDictionary<string, string>) formatter.Deserialize(stream);
            foreach (var kv in attributes)
            {
                writer.SetAttribute(kv.Key, kv.Value);
            }
            
            // TransitDbSnaphot.WriteTo
            var stops = stream.Deserialize<StopId, Stop>(formatter);
            var trips = stream.Deserialize<TripId, Trip>(formatter);
            var connections = stream.Deserialize<ConnectionId, Connection>(formatter);

            // Projects the old, pre-serialization ID onto the new one. Probably the same though
            var stopMapping = new Dictionary<StopId, StopId>();
            foreach (var (stopId, stop) in stops)
            {
                stopMapping[stopId] = writer.AddOrUpdateStop(stop);
            }

            var tripMapping = new Dictionary<TripId, TripId>();
            foreach (var (tripId, trip) in trips)
            {
                tripMapping[tripId] = writer.AddOrUpdateTrip(trip);
            }

            foreach (var (_, c) in connections)
            {
                writer.AddOrUpdateConnection(new Connection(
                    c.GlobalId,
                    stopMapping[c.DepartureStop],
                    stopMapping[c.ArrivalStop],
                    c.DepartureTime,
                    c.TravelTime,
                    c.Mode,
                    tripMapping[c.TripId],
                    c.Attributes
                ));
            }
        }
    }
}