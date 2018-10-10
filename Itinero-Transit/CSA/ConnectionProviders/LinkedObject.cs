using System;
using System.IO;
using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.LinkedData
{
    /**
     * A linked object is an object which has an Uniform Resource Identifier
     */
    [Serializable]
    public abstract class LinkedObject
    {
        // ReSharper disable once MemberCanBeProtected.Global
        public Uri Uri;

        protected LinkedObject(Uri uri)
        {
            Uri = AsUri(uri.ToString());
        }

        /// <summary>
        /// Load all instance fields from a JSON
        /// </summary>
        /// <param name="json"></param>
        protected abstract void FromJson(JObject json);

        /// <summary>
        /// Downloads the resource where this linkedObject points to and tries to instantiate it
        /// </summary>
        /// <returns>The string at the given resource</returns>
        /// <exception cref="FileNotFoundException">If nothing could be downloaded</exception>
        public void Download(JsonLdProcessor loader)
        {
            try
            {
                FromJson((JObject) loader.LoadExpanded(Uri));
            }
            catch (JsonReaderException e)
            {
                Log.Error($"Could not parse {Uri}:\n{e.Message}");
                Log.Error(e.ToString());
                throw;
            }
        }
        

        public static Uri AsUri(string s)
        {
            if (s.StartsWith("https"))
            {
                s = "http" + s.Substring(5);
            }

            return new Uri(s);
        }


        public bool ArrayContains(JArray array, string expected)
        {

            foreach (var elem in array)
            {
                if (elem.IsString() && elem.ToString().Equals(expected))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads 'json["@type"]' (which should be a JArray) and
        /// checks that `expectedType` is one of the members of this array.
        ///
        /// If `expectedType` is not found, an exception is thrown.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="expectedType"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AssertTypeIs(JObject json, string expectedType)
        {
            if (ArrayContains((JArray) json["@type"], expectedType))
            {
                throw new ArgumentException("The passed JSON is not a Linked-Data JSon which follows the LinkedConnections ontology");
            }
        }

        public static string GetValue(JObject json, string uriKey)
        {
            return json[uriKey]["@value"].ToString();
        }

        public static Uri GetId(JObject json, string uriKey)
        {
            return new Uri(json[uriKey]["@id"].ToString());
        }
        
        /// <summary>
        /// Gets json[uriKey][@value] and tries to parse this as an int.
        /// If anything along the way is null, the value 0 is returned.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="uriKey"></param>
        /// <returns></returns>
        public static int GetInt(JToken json, string uriKey)
        {
            return int.Parse(json?[uriKey]?["@value"]?.ToString() ?? "0");
        }


        public Uri Id()
        {
            return Uri;
        }
    }
}