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
    internal class FeedData
    {
        private TimeZoneInfo _timeZone;
        private string _prefix;
        
        public FeedData(IGTFSFeed feed, TimeZoneInfo overrideTimeZone = null)
        {
            Feed = feed;
            _timeZone = overrideTimeZone;
        }

        public IGTFSFeed Feed { get; }

        internal List<string> AgencyUrls()
        {
            return Feed.Agencies.Select(agency => agency.URL).ToList();
        }

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
                
                if (urls == null ||
                    urls.Count == 0)
                {
                    _prefix = string.Empty;
                    return _prefix;
                }
                if (urls.Count > 1)
                {
                    throw new ArgumentException("This GTFS archive contains data on multiple agencies");
                }

                var prefix = urls[0];
                if (!prefix.EndsWith("/"))
                {
                    prefix += "/";
                }

                return _prefix = prefix;
            }
        }

        internal TimeZoneInfo TimeZone
        {
            get
            {
                if (_timeZone != null) return _timeZone;
                
                // try to get timezone info, assume utc if none available.
                var agency = Feed.Agencies.FirstOrDefault();
                if (agency == null ||
                    string.IsNullOrWhiteSpace(agency.Timezone))
                {
                    _timeZone = TimeZoneInfo.Utc;
                }
                else
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
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

        /// <summary>
        /// Returns translation per language.
        /// </summary>
        public Dictionary<string, List<(string language, string translatedTerm)>> Translations { get; } = new Dictionary<string, List<(string language, string translatedTerm)>>();
        
        /// <summary>
        /// Returns which services are scheduled for the given day.
        /// THis uses both 'calenders.txt' and 'calendar_dates.txt' and does keep track of exceptions as well of regular services
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        internal IEnumerable<string> ServicesForDay(DateTime day)
        {
            var doesntGo = GetExceptionallyDoesntDriveToday(day);
            var doesGo = GetExceptionallyDrivesToday(day);

            if (!Feed.Calendars.Any())
            {
                // there is no calendar data, use calendar_dates only.
                // https://developers.google.com/transit/gtfs/reference/#calendar_datestxt
                foreach (var service in doesGo)
                {
                    if (doesntGo.Contains(service)) continue;

                    yield return service;
                }
            }
            else
            { 
                // use week patterns in calendar.
                // https://developers.google.com/transit/gtfs/reference/#calendartxt
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
                            yield return service.ServiceId;
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
                        yield return service.ServiceId;
                    }
                }
            }
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