using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Itinero.IO.LC{
    /// <summary>
    /// The local storage provides serialization to local files
    /// This can be useful to store the entire timetable beforehand and load later on
    /// It acts as a key-value store for serializable objects
    /// </summary>
    public class LocalStorage
    {
        private readonly string _root;

        private static readonly List<string> ForbiddenDirectories =
            new List<string>
            {
                "/",
                "",
                ".",
                ".."
            };

        public LocalStorage(string root)
        {
            
            CheckName(root);
            _root = Path.GetFullPath(root + Path.DirectorySeparatorChar).Normalize();
           
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        
        /// <summary>
        /// Creates a subdirectory in the storage
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public LocalStorage SubStorage(string name)
        {
            CheckName(name);
            return new LocalStorage(_root+"/"+name);
        }

        private static void CheckName(string name)
        {
            if (name.StartsWith("..") || ForbiddenDirectories.Contains(name))
            {
                throw new ArgumentException(
                    $"Using {name} as localstorage is not a good idea, specify a specific directory");
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

                throw new ArgumentException($"Could not read key {key}, wrong type");
            }
        }

        public bool Contains(string key)
        {
            return File.Exists(PathFor(key));
        }

        public List<string> KnownKeys()
        {
            var keys = new List<string>();
            foreach (var path in Directory.EnumerateFiles(_root))
            {
                keys.Add(KeyFor(path));
            }

            keys.Sort();
            return keys;
        }

        // ReSharper disable once MemberCanBePrivate.Global
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
            return _root +
                   key.Replace("_", "_U").Replace("/", "_S");
        }

        private string KeyFor(string path)
        {
            return path.Substring(_root.Length).Replace("_S", "/").Replace("_U", "U");
        }
    }
}