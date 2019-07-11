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
    public interface LinkedObject
    {
        // ReSharper disable once MemberCanBeProtected.Global
        Uri Uri { get; }


        /// <summary>
        /// Load all instance fields from a JSON
        /// </summary>
        /// <param name="json"></param>
        void FromJson(JObject json);
    }

    public static class LinkedObjectExtensions
    {
        /// <summary>
        /// Downloads the resource where this linkedObject points to and tries to instantiate it
        /// </summary>
        /// <returns>The string at the given resource</returns>
        /// <exception cref="FileNotFoundException">If nothing could be downloaded</exception>
        public static void Download(this LinkedObject lo, JsonLdProcessor loader)
        {
            try
            {
                lo.FromJson((JObject) loader.LoadExpanded(lo.Uri));
            }
            catch (JsonReaderException e)
            {
                Log.Error($"Could not parse {lo.Uri}:\n{e.Message}");
                Log.Error(e.ToString());
                throw;
            }
        }
    }
}