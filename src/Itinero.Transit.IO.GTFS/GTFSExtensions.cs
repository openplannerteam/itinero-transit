using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.IO.GTFS.Data;
using Itinero.Transit.Logging;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]
namespace Itinero.Transit.IO.GTFS
{
    internal static class GTFSExtensions
    {
        internal static string IdentifierPrefix(this IGTFSFeed feed)
        {
            var agency = feed.Agencies.FirstOrDefault();
            if (agency?.URL == null) return string.Empty;
            
            var prefix = agency.URL;
            if (!prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            return prefix;
        }

        internal static TimeZoneInfo GetTimeZoneInfo(this IGTFSFeed feed)
        {
            // try to get timezone info, assume utc if none available.
            var agency = feed.Agencies.FirstOrDefault();
            if (agency == null ||
                string.IsNullOrWhiteSpace(agency.Timezone))
            {
                return TimeZoneInfo.Utc;
            }

            return TimeZoneInfo.FindSystemTimeZoneById(agency.Timezone);
        }

        internal static Dictionary<string, DatePattern> GetDatePatterns(this IGTFSFeed feed)
        {
            var datePatterns = new Dictionary<string, DatePattern>();
            
            // sort calendar entities and get first objects.
            using var calendars = feed.Calendars.OrderBy(x => x.ServiceId).GetEnumerator();
            var calendar = calendars.MoveNext() ? calendars.Current : null;
            using var calendarDates = feed.CalendarDates.OrderBy(x => x.ServiceId).ThenBy(x => x.Date).GetEnumerator();
            var calendarDate = calendarDates.MoveNext() ? calendarDates.Current : null;
            
            // loop over calendar entities by increasing service id.
            while (calendar != null || calendarDate != null)
            {
                // determine the next service id.
                string serviceId;
                if (calendar != null && calendarDate != null)
                {
                    serviceId = string.Compare(calendar.ServiceId, calendarDate.ServiceId, StringComparison.Ordinal) < 0
                        ? calendar.ServiceId
                        : calendarDate.ServiceId;
                }
                else if (calendar != null)
                {
                    serviceId = calendar.ServiceId;
                }
                else
                {
                    serviceId = calendarDate.ServiceId;
                }

                // get calendar for current service id if any.
                Calendar currentCalendar = null;
                // skip calendars that are not used.
                while (calendar != null &&
                       string.Compare(calendar.ServiceId, serviceId, StringComparison.Ordinal) < 0)
                {
                    calendar = calendars.MoveNext() ? calendars.Current : null;
                }

                // loop over calendars for the current service id.
                while (calendar != null &&
                       calendar.ServiceId == serviceId)
                {
                    if (currentCalendar != null)
                        Log.Warning($"Multiple calendar objects found for service id: {calendar.ServiceId}");
                    currentCalendar = calendar;
                    //Log.Verbose($"Calendar - [{currentCalendar.ServiceId}]: {currentCalendar}");
                    calendar = calendars.MoveNext() ? calendars.Current : null;
                }

                // create date pattern for the current service id.
                var weekPattern = WeekPattern.From(currentCalendar);
                var datePattern = new DatePattern(weekPattern);
                datePatterns[serviceId] = datePattern;

                // skip calendar dates with unused service ids.
                while (calendarDate != null &&
                       string.Compare(calendarDate.ServiceId, serviceId, StringComparison.Ordinal) < 0)
                {
                    //Log.Verbose($"CalendarDate - [{calendarDate.ServiceId}]: {calendarDate} (SKIPPED)");
                    calendarDate = calendarDates.MoveNext() ? calendarDates.Current : null;
                }

                // loop over calendar dates with current service id.
                while (calendarDate != null &&
                       calendarDate.ServiceId == serviceId)
                {
                    //Log.Verbose($"CalendarDate - [{calendarDate.ServiceId}]: {calendarDate}");
                    
                    // update date pattern.
                    datePattern.AddException(calendarDate.Date, calendarDate.ExceptionType == ExceptionType.Added);
                    
                    calendarDate = calendarDates.MoveNext() ? calendarDates.Current : null;
                }
            }

            return datePatterns;
        }

        internal static Dictionary<string, Route> GetRoutes(this IGTFSFeed feed)
        {
            return feed.Routes.ToDictionary(x => x.Id);
        }

        internal static IEnumerable<(string id, Itinero.Transit.Data.Core.Stop stop)> GetStops(this IGTFSFeed feed, string idPrefix = null,
            Func<string, IEnumerable<(string lng, string term)>> translate = null)
        {
            idPrefix ??= string.Empty;
            
            foreach (var stop in feed.Stops)
            {
                // build the id.
                var id = stop.Url;
                if (string.IsNullOrEmpty(id))
                {
                    id = idPrefix + "stop/" + stop.Id;
                }

                // collect attributes.
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

                // add translated names.
                if (!string.IsNullOrEmpty(stop.Name) &&
                    translate != null)
                {
                    var translated = translate(stop.Name);
                    foreach (var (lng, term) in translated)
                    {
                        attributes["name:" + lng] = term;
                    }
                }

                yield return (stop.Id, new Itinero.Transit.Data.Core.Stop(id, (stop.Longitude, stop.Latitude), attributes));
            }
        }
    }
}