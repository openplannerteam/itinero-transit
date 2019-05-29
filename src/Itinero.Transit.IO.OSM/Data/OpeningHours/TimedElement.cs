using System;
using System.Globalization;

namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    /// <summary>
    /// A timed element is a cyclical element (in time) which is used to describe time patterns (esp. opening hours).
    /// It triggers at a well-defined moment of an infinitely short duration.
    /// 
    /// E.g: An element 'Su' (describing every sunday) will trigger at 00:00 every sunday.
    /// An element such as '10:00' will trigger at 10 o' clock.
    ///
    /// At last, some rules will combine two triggers, e.g. Mo-Fr (including both) will use the of monday-trigger and friday+24H trigger to stop
    ///
    /// To make working with the timed elements practical, they all offer a method to determine the next and previous trigger dates, allowing to enumerate
    /// 
    /// </summary>
    public interface ITimedElement
    {
        DateTime Next(DateTime dt);
        DateTime Prev(DateTime dt);
    }

    public static class TimedElements
    {
        public static ITimedElement MomentOfDay(int hours, int minutes)
        {
            return
                new HourOfDayEvent(hours).Chain(new MinuteOfDayEvent(minutes));
        }

        public static ITimedElement MonthOfYear(int month)
        {
            return new MonthOfYearEvent(month);
        }

        public static ITimedElement Offset(this ITimedElement el, TimeSpan offset)
        {
            return new TimedOffset(el, offset);
        }

        public static ITimedElement Easter()
        {
            return new Easter();
        }

        public static ITimedElement Chain(this ITimedElement biggest, ITimedElement el)
        {
            return new Chain(biggest, el);
        }

        public static ITimedElement DayOfMonth(int d)
        {
            return new DayOfMonthEvent(d);
        }

        public static ITimedElement DayOfWeek(int d)
        {
            return new DayOfWeekEvent(d);
        }

        public static ITimedElement Date(int m, int d)
        {
            return MonthOfYear(m)
                .Chain(DayOfMonth(d));
        }
    }


    public class TimedOffset : ITimedElement
    {
        private readonly ITimedElement _timedElementImplementation;
        private readonly TimeSpan _offset;

        public TimedOffset(ITimedElement timedElementImplementation, TimeSpan offset)
        {
            _timedElementImplementation = timedElementImplementation;
            _offset = offset;
        }


        public DateTime Next(DateTime dt)
        {
            return _timedElementImplementation.Next(dt - _offset);
        }

        public DateTime Prev(DateTime dt)
        {
            return _timedElementImplementation.Prev(dt - _offset);
        }
    }

    public class Skip : ITimedElement
    {
        private readonly ITimedElement _bigTick, _smallTick;
        private readonly int _skip;

        public Skip(ITimedElement bigTick, ITimedElement smallTick, int skip)
        {
            _bigTick = bigTick;
            _smallTick = smallTick;
            _skip = skip;
        }


        public DateTime Next(DateTime dt)
        {
            dt = _bigTick.Next(dt);
            for (int i = 0; i < _skip; i++)
            {
                dt = _smallTick.Next(dt);
            }

            return dt;
        }

        public DateTime Prev(DateTime dt)
        {
            dt = _bigTick.Prev(dt);
            for (int i = 0; i < _skip; i++)
            {
                dt = _smallTick.Prev(dt);
            }

            return dt;
        }
    }

    public class Chain : ITimedElement
    {
        private readonly ITimedElement _a;
        private readonly ITimedElement _b;

        /// <summary>
        /// The cycle of 'a' should encompass at least the cycle of 'b'
        /// This can be used e.g. to combine 'hour' and 'minutes'
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Chain(ITimedElement a, ITimedElement b)
        {
            _a = a;
            _b = b;
        }

        public DateTime Next(DateTime dt)
        {
            // Consider:
            // _a = at 10 o' clock
            // _b = at half past (something)
            // We navigate first to the next 10 o'clock, then to the next half past:
            dt = _a.Next(dt);
            return _b.Next(dt);
        }

        public DateTime Prev(DateTime dt)
        {
            // Consider:
            // _a = at 10 o' clock
            // _b = at half past (something)

            // 10:20 -> (10:00; 09:30) -> 10:30 (previous day)
            // 10:40 -> (10:00; 10:30) -> 10:30
            // 11:40 -> (11:00; 11:30) -> 10:30

            // we navigate to the closest previous time
            dt = _b.Prev(dt);
            return _a.Prev(dt);
        }
    }

    public class WeekOfYear : ITimedElement
    {
        private readonly int _week;

        public WeekOfYear(int week)
        {
            _week = week;
        }

        private int CurrentWeek(DateTime dt)
        {
            return (dt.DayOfYear - (((int) dt.DayOfWeek + 6) % 7) + 11) / 7;
        }

        private static DateTime DateFromWeekNumber(int year, int weekNumber, int dayOfWeek = 0)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Tuesday - jan1.DayOfWeek;

            DateTime firstMonday = jan1.AddDays(daysOffset);

            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(jan1, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekNumber;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }

            var result = firstMonday.AddDays(weekNum * 7 + dayOfWeek - 1);
            return result;
        }

        public DateTime Next(DateTime dt)
        {
            var t = DateFromWeekNumber(dt.Year, _week);
            if (t <= dt)
            {
                return DateFromWeekNumber(dt.Year + 1, _week);
            }

            return t;
        }

        public DateTime Prev(DateTime dt)
        {
            var t = DateFromWeekNumber(dt.Year, _week);
            if (t >= dt)
            {
                return DateFromWeekNumber(dt.Year - 1, _week);
            }

            return t;
        }
    }

    public class MonthOfYearEvent : ITimedElement
    {
        private readonly int _month;

        /// <summary>
        /// Triggers when the month begins.
        /// Note: months are one-indexed (january = 01, december = 12)
        /// </summary>
        /// <param name="month"></param>
        public MonthOfYearEvent(int month)
        {
            _month = month;
        }


        public DateTime Next(DateTime dt)
        {
            var date = new DateTime(dt.Year, _month, 1);
            if (date <= dt)
            {
                return new DateTime(dt.Year + 1, _month, 1);
            }

            return date;
        }

        public DateTime Prev(DateTime dt)
        {
            var date = new DateTime(dt.Year, _month, 1);
            if (date >= dt)
            {
                return new DateTime(dt.Year - 1, _month, 1);
            }

            return date;
        }
    }

    public class DayOfMonthEvent : ITimedElement
    {
        private readonly int _day;

        public DayOfMonthEvent(int day)
        {
            _day = day;
        }


        public DateTime Next(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, _day, 0, 0, 0);
            if (t <= dt)
            {
                return t.AddMonths(1);
            }

            return t;
        }

        public DateTime Prev(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, _day, 0, 0, 0);
            if (t <= dt)
            {
                return t.AddMonths(-1);
            }

            return t;
        }
    }


    public class HourOfDayEvent : ITimedElement
    {
        private readonly int _hour;

        public HourOfDayEvent(int hour)
        {
            _hour = hour;
        }


        public DateTime Next(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, dt.Day, _hour, 0, 0);
            if (t <= dt)
            {
                return new DateTime(dt.Year, dt.Month, dt.Day + 1, _hour, 0, 0);
            }

            return t;
        }

        public DateTime Prev(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, dt.Day, _hour, 0, 0);
            if (t >= dt)
            {
                return new DateTime(dt.Year, dt.Month, dt.Day - 1, _hour, 0, 0);
            }

            return t;
        }
    }

    public class DayOfWeekEvent : ITimedElement
    {
        private readonly int _weekday;

        public DayOfWeekEvent(int weekday)
        {
            // Monday = 0 but has to become 1
            // Sunday = 6 but has to become 0
            _weekday = (weekday + 8) % 7;
        }


        public DateTime Next(DateTime dt)
        {
            dt = dt.Date.AddDays(1);
            while ((int) dt.DayOfWeek != _weekday)
            {
                dt = dt.AddDays(1);
            }

            return dt;
        }

        public DateTime Prev(DateTime dt)
        {
            dt = dt.Date;
            while ((int) dt.DayOfWeek != _weekday)
            {
                dt = dt.AddDays(-1);
            }

            return dt;
        }
    }

    public class MinuteOfDayEvent : ITimedElement
    {
        private readonly int _minute;

        public MinuteOfDayEvent(int minute)
        {
            _minute = minute;
        }


        public DateTime Next(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, _minute, 0);
            if (t <= dt)
            {
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour + 1, _minute, 0);
            }

            return t;
        }

        public DateTime Prev(DateTime dt)
        {
            var t = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, _minute, 0);
            if (t >= dt)
            {
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour - 1, _minute, 0);
            }

            return t;
        }
    }

    public class Easter : ITimedElement
    {
        private static DateTime EasterOfYear(int year)
        {
            // https://stackoverflow.com/questions/2510383/how-can-i-calculate-what-date-good-friday-falls-on-given-a-year

            var g = year % 19;
            var c = year / 100;
            var h = (c - c / 4 - (8 * c + 13) / 25 + 19 * g + 15) % 30;
            var i = h - h / 28 * (1 - h / 28 * (29 / (h + 1)) * ((21 - g) / 11));

            var day = i - ((year + year / 4 + i + 2 - c + c / 4) % 7) + 28;
            var month = 3;

            // ReSharper disable once InvertIf
            if (day > 31)
            {
                month++;
                day -= 31;
            }

            return new DateTime(year, month, day);
        }

        public DateTime Next(DateTime dt)
        {
            var t = EasterOfYear(dt.Year);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (t <= dt)
            {
                return EasterOfYear(dt.Year + 1);
            }

            return t;
        }

        public DateTime Prev(DateTime dt)
        {
            var t = EasterOfYear(dt.Year);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (t >= dt)
            {
                return EasterOfYear(dt.Year - 1);
            }

            return t;
        }
    }
}