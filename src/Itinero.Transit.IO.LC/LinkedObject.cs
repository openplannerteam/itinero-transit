using System;
using System.IO;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.IO.LC
{
    /// <summary>
    /// A linked object is an object which has an Uniform Resource Identifier.
    /// The linked object should be initialised with the URI.
    /// 
    /// Then, the attributes should be set via the 'FromJson' object. This JSON can be provided directly or via the 'Download'method
    /// </summary>
    [Serializable]
    public abstract class LinkedObject
    {
        // ReSharper disable once MemberCanBeProtected.Global
        public readonly Uri Uri;

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
            return new Uri(s);
        }

        public Uri Id()
        {
            return Uri;
        }
    }
}