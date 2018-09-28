using System;

namespace Itinero_Transit.CSA
{
    
    /// <summary>
    /// The connections-provider is an object responsible for giving all kinds of connections.
    /// It is able to provide connections for
    /// - Public transport connections from different providers
    /// - Internal transfers
    /// - Multimodal transfers (with walking/cycling)
    ///
    /// The connection provider is trip-aspecific and can be reused.
    /// Although the algorithms can be run with a few general subproviders (say: SNCB, De Lijn + Walking),
    /// ConnectionsProviders can be highly specific (e.g. foldable bike, private shuttle services, ...)
    ///
    /// </summary>
    public interface IConnectionsProvider
    {


        IConnection GetConnection(Uri id);
        


    }
}