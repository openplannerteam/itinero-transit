using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.LinkedData
{
    /**
     * A linked object is an object which has an Uniform Resource Identifier
     */
    public abstract class LinkedObject
    {
        public Uri Uri { get; set; }

        public LinkedObject(Uri uri)
        {
            this.Uri = uri;
        }

        /// <summary>
        /// Downloads the resource where this linkedObject points to.
        /// This method might implement caching, ... in the future
        /// </summary>
        /// <returns>The string at the given resoruce</returns>
        /// <exception cref="FileNotFoundException">If nothing could be downloaded</exception>
        public string Download()
        {
            return Downloader.Download(Uri); // Do the actual stuff
        }


    }
}