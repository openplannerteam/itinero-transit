namespace Itinero.Transit.Data.OpeningHoursRDParser
{
    /// <summary>
    /// A timed element is a cyclical element (in time) which is used to describe time patterns (esp. opening hours).
    /// It can trigger a state UP, DOWN.
    /// E.g: An element 'Su' (describing every sunday) will trigger UP at 00:00 every sunday and DOWN at 23:59:59.9999999...
    /// An element such as '10:00' will trigger UP at 10 o' clock and DOWN a minute later.
    ///
    /// At last, some rules will combine two triggers, e.g. Mo-Fr will use the 'UP'-trigger of monday and the 'DOWN' trigger of Friday to depict the timerange.
    ///
    /// To make working with the timed elements practical, they all offer a method to determine what the next change will be
    /// 
    /// </summary>
    public class TimedElement
    {
        
    }
}