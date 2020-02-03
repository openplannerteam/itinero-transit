using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Itinero.Transit.Data.Serialization
{
    public static class DatabaseSerializer
    {
        public static void Serialize<TId, T>(this Stream stream, IDatabaseReader<TId, T> db, IFormatter formatter)
            where TId : InternalId, new() where T : IGlobalId
        {
            formatter.Serialize(stream, db.Count());
            foreach (var t in db)
            {
                formatter.Serialize(stream, db.GetId(t));
                formatter.Serialize(stream, t);
            }
        }

        public static List<(TId, T)> Deserialize<TId, T>(this Stream stream, IFormatter formatter)
        {
            var count = (int) formatter.Deserialize(stream);
            var result = new List<(TId tid, T t)>();
            for (var i = 0; i < count; i++)
            {
                var tid = (TId) formatter.Deserialize(stream);
                var t = (T) formatter.Deserialize(stream);
                result.Add((tid, t));
            }

            return result;
        }
    }
}