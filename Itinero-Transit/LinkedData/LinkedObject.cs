﻿using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.LinkedData
{
    /**
     * A linked object is an object which has an Uniform Resource Identifier
     */
    public abstract class LinkedObject
    {
        public Uri Uri { get; set; }

        protected LinkedObject(Uri uri)
        {
            Uri = AsUri(uri.ToString());
        }

        /// <summary>
        /// Load all instance fields from a JSON
        /// </summary>
        /// <param name="json"></param>
        protected abstract void FromJson(JToken json);
        
        /// <summary>
        /// Downloads the resource where this linkedObject points to and tries to instantiate it
        /// </summary>
        /// <returns>The string at the given resource</returns>
        /// <exception cref="FileNotFoundException">If nothing could be downloaded</exception>
        public void Download()
        {
            Log.Information($"Downloading {Uri}");
            FromJson(Downloader.DownloadJson(Uri));
        }


        public static Uri AsUri(string s)
        {
            if (!s.StartsWith("https") && s.StartsWith("http"))
            {
                s = "https" + s.Substring(4);
            }
            return new Uri(s);
        }

    }
}