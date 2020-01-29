using GTFS.Entities;

namespace Itinero.Transit.IO.GTFS.Data
{
    internal struct WeekPattern
    {
        public bool Monday { get; set; }
        
        public bool Tuesday { get; set; }
        
        public bool Wednesday { get; set; }

        public bool Thursday { get; set; }

        public bool Friday { get; set; }
        
        public bool Saturday { get; set; }
        
        public bool Sunday { get; set; }

        public static WeekPattern? From(Calendar calendar)
        {
            if (calendar == null) return null;
            
            return new WeekPattern()
            {
                Monday =  calendar.Monday,
                Tuesday = calendar.Tuesday,
                Wednesday = calendar.Wednesday,
                Thursday = calendar.Thursday,
                Friday = calendar.Friday,
                Saturday = calendar.Saturday,
                Sunday = calendar.Sunday
            };
        }
    }
}