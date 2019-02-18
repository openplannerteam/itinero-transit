Once the [core concepts](index.md) are clear, using the library in your project should be quite easy.

Performing route planning queries (at the bare minimum) consists of the following parts:

1. Setting up a provider
2. Setting up a profile
3. Asking a query


Setting up a provider
---------------------

As noted, the library needs a server which offers all the information as [Linked Connections](linkedconnections.org).
A linked Connections dataset has two entry points:

 - A web resource which contains all stops
 - Multiple web resources which contain the connections in a timewindow (with a link to a next and previous timetable)

Search for these links (or ask for them) on your public transport provider website.

Once you have obtained these links, make a new dataset with:


        using Itinero.Transit.IO.LC;
        
        ...
        
        Uri locations = <...>
        Uri connections = <...>
        var dataset = new LinkedConnectionDataset(locations, connections);
        var transitDB = dataset.AsTransitDb();


If needed, data can already be downloaded now for a certain time period. For example, if the code should run on a server, it can be useful to prefetch the data for the coming days. If it is not certain when the traveller will travel (if he'll travel at all), the next line should be skipped:

        transitDB.UpdateTimeFrame(startDate, endDate)        

If you'd like to force an update on the data already in the transit db, use the refresh flag:

        transitDB.UpdateTimeFrame(startDate, endDate, refresh:true)        


Note that *needed data will be downloaded automatically upon request*. In other words, if a traveller want to travel at a time span that is not yet loaded, it'll be loaded dynamically.

Setting up a profile
--------------------

The *profile* contains personal preferences of the traveller, such as:

- Which public transport providers are available
- The walking time between stops
- How much time is needed to get from one vehicle to another
- Which metrics are used to compare journeys

A profile is constructed with:

        var p = new Profile
                <TransferStats>
                    (
                    State.TransitDb,
                    new InternalTransferGenerator(internalTransferTime),
                    null,
                    TransferStats.Factory,
                    TransferStats.ProfileTransferCompare);
