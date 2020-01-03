using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;

namespace Itinero.Transit.Tests.Functional.Utils
{
    /// <summary>
    /// Small static class which fetches TransitDbs from disks which are required by some of the tests
    /// </summary>
    public static class TransitDbCache
    {
        private static readonly Dictionary<(string, uint), TransitDb> _tdbCache
            = new Dictionary<(string, uint), TransitDb>();

        public static TransitDb Get(string path, uint index)
        {
            var key = (path, index);
            if (!_tdbCache.ContainsKey(key))
            {
                var tdb = new TransitDb(index);
                var wr = tdb.GetWriter();
                wr.ReadFrom(path);
                wr.Close();
                _tdbCache[key] = tdb;
            }

            return _tdbCache[key];
        }

        public static List<TransitDb> GetAll(List<string> paths)
        {
            var result = new List<TransitDb>();
            for (var i = 0; i < paths.Count; i++)
            {
                result.Add(Get(paths[i], (uint) i));
            }

            return result;
        }
    }
}