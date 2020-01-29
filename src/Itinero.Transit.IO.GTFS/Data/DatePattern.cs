using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]
namespace Itinero.Transit.IO.GTFS.Data
{
    internal class DatePattern
    {
        private readonly WeekPattern? _weekPattern;
        private Dictionary<DateTime, bool> _exceptions;

        public DatePattern(WeekPattern? weekPattern = null)
        {
            _weekPattern = weekPattern;
        }

        public void AddException(DateTime date, bool included)
        {
            if (date.Date != date) throw new ArgumentOutOfRangeException(nameof(date), $"Only dates without a time component are allowed.");

            if (_exceptions == null) _exceptions = new Dictionary<DateTime, bool>();
            _exceptions[date] = included;
        }

        public bool IsActiveOn(DateTime date)
        {
            if (date.Date != date) throw new ArgumentOutOfRangeException(nameof(date), $"Only dates without a time component are allowed.");

            var active = false;
            if (_weekPattern != null)
            {
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        active = _weekPattern.Value.Monday;
                        break;
                    case DayOfWeek.Tuesday:
                        active = _weekPattern.Value.Tuesday;
                        break;
                    case DayOfWeek.Wednesday:
                        active = _weekPattern.Value.Wednesday;
                        break;
                    case DayOfWeek.Thursday:
                        active = _weekPattern.Value.Thursday;
                        break;
                    case DayOfWeek.Friday:
                        active = _weekPattern.Value.Friday;
                        break;
                    case DayOfWeek.Saturday:
                        active = _weekPattern.Value.Saturday;
                        break;
                    case DayOfWeek.Sunday:
                        active = _weekPattern.Value.Sunday;
                        break;
                }
            }

            if (_exceptions == null || !_exceptions.TryGetValue(date, out var exception)) return active;

            return exception;
        }
    }
}