using System;
using Itinero_Transit.LinkedData;

namespace Itinero_Transit.CSA
{
    /**
     * A connection represents a single connection someone can take.
     * It consists of a departure and arrival stop, departure and arrival time.
     * Note that a connection does _never_ have intermediate stops.
     *
     */
    public class Connection : LinkedObject
    {
        public Connection(Uri uri) : base(uri)
        {
        }
    }
}