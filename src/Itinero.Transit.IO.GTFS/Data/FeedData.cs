using System;
using System.Collections.Generic;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;

namespace Itinero.Transit.IO.GTFS.Data
{
    /// <summary>
    /// Feed data contains all data of a feed and some logic to fetch and cache this data.
    /// All have a cached and non-cached version
    /// </summary>
    public class FeedData
    {
        private readonly string _path;

        public FeedData(string path)
        {
            _path = path;
        }


        private GTFSFeed _feed;

        public GTFSFeed Feed
        {
            get
            {
                if (_feed == null)
                {
                    Log.Information("Starting to read the GTFS-archive");
                    _feed = new GTFSReader<GTFSFeed>().Read(_path);
                    Log.Information("GTFS archive unpacked");
                }

                return _feed;
            }
        }


        internal List<string> AgencyUrls()
        {
            return Feed.Agencies.Get().ToList().Select(agency => agency.URL).ToList();
        }

        private string _prefix;

        /// <summary>
        /// Get the identifier-prefix for this GTFS feed.
        /// The identifier-prefix starts with the agencies website ('http://belgiantrain.be/') and has a trailing slash.
        ///
        /// If the GTFS feed contains multiple agencies, an error is thrown
        /// </summary>
        /// <returns></returns>
        public string IdentifierPrefix
        {
            get
            {
                if (_prefix != null)
                {
                    return _prefix;
                }

                var urls = AgencyUrls();
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
        }

        private TimeZoneInfo _timeZone;

        /// <summary>
        /// Get the identifier-prefix for this GTFS feed.
        /// The identifier-prefix starts with the agencies website ('http://belgiantrain.be/') and has a trailing slash.
        ///
        /// If the GTFS feed contains multiple agencies, an error is thrown
        /// </summary>
        /// <returns></returns>
        public TimeZoneInfo TimeZone
        {
            get
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (_timeZone == null)
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById(Feed.Agencies.Get(0).Timezone);
                }

                return _timeZone;
            }
        }

        private Dictionary<string, List<Trip>> _serviceIdToTrip;

        public Dictionary<string, List<Trip>> ServiceIdToTrip
        {
            get
            {
                if (_serviceIdToTrip != null)
                {
                    return _serviceIdToTrip;
                }


                _serviceIdToTrip = new Dictionary<string, List<Trip>>();

                foreach (var gtfstrip in Feed.Trips)
                {
                    if (!_serviceIdToTrip.ContainsKey(gtfstrip.ServiceId))
                    {
                        _serviceIdToTrip[gtfstrip.ServiceId] = new List<Trip>();
                    }

                    _serviceIdToTrip[gtfstrip.ServiceId].Add(gtfstrip);
                }


                return _serviceIdToTrip;
            }
        }


        public Dictionary<string, List<(string language, string translatedTerm)>>
            Translations
        {
            get
            {
                var result = new Dictionary<string, List<(string language, string translatedTerm)>>();

                // TODO ADD REAL TRANSLATIONS AND A REAL TEST
                result["Bruges"] = new List<(string language, string translatedTerm)>
                {
                    ("nl", "Brugge"),
                    ("fr", "Bruges"),
                    ("es", "Brugas"),
                    ("en", "Bruges"),
                    ("de", "Brügge")
                };
                return result;
            }
        }
        /// <summary>


        /// Returns which services are scheduled for the given day.
        /// THis uses both 'calenders.txt' and 'calendar_dates.txt' and does keep track of exceptions as well of regular services
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public List<Calendar> ServicesForDay(DateTime day)
        {
            var services = new List<Calendar>();
            var doesntGo = GetExceptionallyDoesntDriveToday(day);
            var doesGo = GetExceptionallyDrivesToday(day);

            foreach (var service in Feed.Calendars)
            {
                if (doesntGo.Contains(service.ServiceId))
                {
                    continue;
                }

                if (doesGo.Contains(service.ServiceId))
                {
                    if (service.StartDate <= day && day <= service.EndDate)
                    {
                        services.Add(service);
                    }

                    continue;
                }

                var goesToday =
                    service[day.DayOfWeek] &&
                    service.StartDate <= day &&
                    day <= service
                        .EndDate; // end date is included in the interval: https://developers.google.com/transit/gtfs/reference#calendartxt
                if (goesToday)
                {
                    services.Add(service);
                }
            }


            return services;
        }


        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly HashSet<string> _empty = new HashSet<string>();
        private Dictionary<DateTime, HashSet<string>> _exceptionallyDrivesByDate;
        private Dictionary<DateTime, HashSet<string>> _exceptionallyDoesntDriveByDate;


        private void ReadCalendarDates()
        {
            _exceptionallyDrivesByDate = new Dictionary<DateTime, HashSet<string>>();
            _exceptionallyDoesntDriveByDate = new Dictionary<DateTime, HashSet<string>>();

            foreach (var exception in Feed.CalendarDates)
            {
                var key = exception.Date;

                if (exception.ExceptionType == ExceptionType.Added)
                {
                    _exceptionallyDrivesByDate.AddTo(key, exception.ServiceId);
                }
                else
                {
                    _exceptionallyDoesntDriveByDate.AddTo(key, exception.ServiceId);
                }
            }
        }

        /// <summary>
        /// Based on calendar_dates.txt only, gives the services that exceptionally go today
        /// </summary>
        public HashSet<string> GetExceptionallyDrivesToday(DateTime date)
        {
            if (_exceptionallyDrivesByDate == null)
            {
                ReadCalendarDates();
            }

            if (_exceptionallyDrivesByDate.TryGetValue(date, out var drivesToday))
            {
                return drivesToday;
            }

            return _empty;
        }

        /// <summary>
        /// Based on calendar_dates.txt only, gives the services that exceptionally don't go today
        /// </summary>
        public HashSet<string> GetExceptionallyDoesntDriveToday(DateTime date)
        {
            if (_exceptionallyDoesntDriveByDate == null)
            {
                ReadCalendarDates();
            }

            if (_exceptionallyDoesntDriveByDate.TryGetValue(date, out var drivesToday))
            {
                return drivesToday;
            }

            return _empty;
        }
    }
}