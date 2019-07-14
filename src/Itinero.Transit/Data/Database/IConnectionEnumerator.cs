using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data
{
    
    /// <summary>
    /// A time aware enumerator for connections.
    /// It has a special form to ensure efficiency
    /// </summary>
    public interface IConnectionEnumerator
    {
        /// <summary>
        /// Moves the enumerator to the given date.
        /// HasNext or HasPrevious will then correctly move the enumerator to the next or previous entry - if any
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        void MoveTo(ulong dateTime);

        
        /// <summary>
        /// PURE (except that it changes things in 'toWrite')
        /// Copies the information on the 'current' index into 'toWrite'
        /// </summary>
        /// <returns></returns>
        bool Current(Connection toWrite);

        /// <summary>
        /// NONPURE
        /// Determines what the next connection to scan is.
        /// If not found, returns true
        /// </summary>
        /// <returns></returns>
        bool HasNext();

        bool HasPrevious();

        /// <summary>
        /// THe current time this enumerator points to. Only valid after calling 
        /// </summary>
        ulong CurrentDateTime { get; }
    }

    public static class ConnectionEnumeratorExtensions
    {
        public static Connection Current(this IConnectionEnumerator ice)
        {
            var c = new Connection();
            ice.Current(c);
            return c;
        }
    }

    
}