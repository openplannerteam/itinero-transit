Once the [core concepts](index.md) are clear, using the library in your project should be quite easy.

**Important**: This section is subject to change! While the big outlines will remaint the same, we will streamline the code below. Expect breaking changes in the coming months

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
                <TransferStats>         // The metrics used, here total travel time and number of transfers are minimized
                    (
                    
                    // The time it takes to transfer from one train to another (and the underlying algorithm, in this case: always the same time)
                    new InternalTransferGenerator(180 /*seconds*/), 

                    // The intermodal stop algorithm. Note that a transitDb is used to search stop location
                    new CrowsFlightTransferGenerator(transitDb, maxDistance: 500 /*meter*/,  walkingSpeed: 1.4 /*meter/second*/),
                    
                    // The object that can create a metric
                    TransferStats.Factory,
                    
                    // The comparison between routes. _This comparison should check if two journeys are covering each other, as seen in core concepts!_
                    TransferStats.ProfileTransferCompare
                    ); 
                    
Asking a query
--------------

Now that all data is gathered, a journey can be requested with:

        var possibleJourneys = transitDB.CalculateJourneys<TransferStats>
            (p,
            from, // a string containing the URI for the departure station
            to, // a string containing the URI for the arrival station
            DateTime? departure = null,     // The (earliest) departure time
            DateTime? arrival = null        // The (latest) arrival time
            );         

This will give a list of journeys.

There are more options available (such as Isochrone lines, Earliest arriving journey or latest arriving journy).
For a full overview, refer to [the generated docs](/_site/api/Itinero.Transit.Algorithms.CSA.ProfileExtensions.html).
