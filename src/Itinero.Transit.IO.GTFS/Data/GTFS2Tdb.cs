using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GTFS;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.IO.GTFS.Data
{
    public class Gtfs2Tdb
    {
        private readonly string _path;

        private GTFSFeed _feed;

        private GTFSFeed Feed
        {
            get
            {
                if (_feed == null)
                {
                    _feed = new GTFSReader<GTFSFeed>().Read(_path);
                }

                return _feed;
            }
        }

        public Gtfs2Tdb(string path)
        {
            _path = path;
        }

        private void LoadGtfs()
        {
        }

        internal void AddConnections(TransitDbWriter writer)
        {
            foreach (var calendar in Feed.Calendars)
            {
                AddServiceForDay(calendar.StartDate, calendar.ServiceId);
            }
        }

        internal void AddServiceForDay(DateTime date, string serviceId)
        {
           var trip =  Feed.Trips.Get(serviceId);
        }


        internal Dictionary<string, StopId> AddLocations(TransitDbWriter writer)
        {
            var gtfsId2TdbId = new Dictionary<string, StopId>();

            foreach (var stop in Feed.Stops.Get())
            {
                var id = stop.Url ?? IdentifierPrefix() + "stop/" + stop.Id;

                var attributes = new Dictionary<string, string>
                {
                    {"name", stop.Name},
                    {"code", stop.Code},
                    {"description", stop.Description},
                    {"parent_station", stop.ParentStation},
                    {"platform", stop.PlatformCode},
                    {"levelid", stop.LevelId},
                    {"wheelchairboarding", stop.WheelchairBoarding},
                    {"zone", stop.Zone},
                };


                var stopId = writer.AddOrUpdateStop(
                    new Stop(id, (stop.Longitude, stop.Latitude), attributes));
                gtfsId2TdbId.Add(stop.Id, stopId);
            }

            return gtfsId2TdbId;
        }


        private string _prefix;

        /// <summary>
        /// Get the identifier-prefix for this GTFS feed.
        /// The identifier-prefix starts with the agencies website ('http://belgiantrain.be/') and has a trailing slash.
        ///
        /// If the GTFS feed contains multiple agencies, an error is thrown
        /// </summary>
        /// <returns></returns>
        internal string IdentifierPrefix()
        {
            if (_prefix != null)
            {
                return _prefix;
            }

            var urls = AgencyURLS();
            if (urls.Count > 1)
            {
                throw new ArgumentException("The GTFS archive " + _path + " contains data on multiple agencies");
            }

            var prefix = urls[0];
            if (!prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            return _prefix = prefix;
        }

        internal List<string> AgencyURLS()
        {
            return Feed.Agencies.Get().ToList().Select(agency => agency.URL).ToList();
        }
    }
}