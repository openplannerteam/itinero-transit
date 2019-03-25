namespace Itinero.Transit.Data
{
    public static class ConnectionExtensions
    {

        public static bool CanGetOn(this IConnection c)
        {
            var m = (c.Mode % 4);
            return m == 0 || m == 1;
        }
        
        public static bool CanGetOff(this IConnection c)
        {
            var m = (c.Mode % 4);
            return m == 0 || m == 2;
        }
        
        
    }
}