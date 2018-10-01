using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Itinero_Transit.CSA.Data
{
    /// <summary>
    /// The local storage provides serialization to local files
    /// This can be usefull to store the entire timetable beforehand and load later on
    /// It acts as a key-value store for serializable objects
    /// </summary>
    public class LocalStorage
    {
        private readonly string root;

        private static readonly List<string> ForbiddenDirectories =
            new List<string>()
            {
                "/",
                "",
                ".",
                ".."
            };

        public LocalStorage(string root)
        {
            this.root = Path.GetFullPath(root + Path.DirectorySeparatorChar).Normalize();
            if (ForbiddenDirectories.Contains(root))
            {
                throw new ArgumentException(
                    $"Using {root} as localstorage is not a good idea, specify a specific directory");
            }

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        /// <summary>
        /// Stores the given value under the associated key.
        /// Returns the value
        /// </summary>
        /// <returns>The unmodified value</returns>
        public T Store<T>(string key, T value)
        {
            using (var fs = File.OpenWrite(PathFor(key)))
            {
                var wr = new BinaryFormatter();
                wr.Serialize(fs, value);
            }

            return value;
        }

        public T Retrieve<T>(string key)
        {
            using (var fs = File.OpenRead(PathFor(key)))
            {
                var wr = new BinaryFormatter();
                var x = wr.Deserialize(fs);
                if (x is T item)
                {
                    return item;
                }
                else
                {
                    throw new ArgumentException($"Could not read key {key}, wrong type");
                }
            }
        }

        public bool Contains(string key)
        {
            return File.Exists(PathFor(key));
        }

        public List<string> KnownKeys()
        {
            var keys = new List<string>();
            foreach (var path in Directory.EnumerateFiles(root))
            {
                keys.Add(KeyFor(path));
            }

            keys.Sort();
            return keys;
        }

        public void RemoveKey(string key)
        {
            File.Delete(PathFor(key));
        }

        public void ClearAll()
        {
            foreach (var key in KnownKeys())
            {
                RemoveKey(key);
            }
        }

        private string PathFor(string key)
        {
            return root +
                   key.Replace("_", "_U").Replace("/", "_S");
        }

        private string KeyFor(string path)
        {
            return path.Substring(root.Length).Replace("_S", "/").Replace("_U", "U");
        }
    }
}