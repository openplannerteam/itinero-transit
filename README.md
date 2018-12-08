# itinero-transit

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:anyways_Openplannerteam_ItineroTransit)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=anyways_Openplannerteam_ItineroTransit)  

This is a C# implementation of a client consuming [Linked Connections](https://linkedconnections.org/) to plan transit routes. This is going to replace the currently unfinished [transit module](https://github.com/itinero/transit) in [tinero](http://www.itinero.tech/).

## TransitDb

We have a slightly different approach than the other LC clients. We don't use LCs directly but we use an intermediate data structure between the route planning algorithms (CSA) and the source of the connections, being linked connections.

The TransitDb is:
- A cache for connections    
- A highly optimized data structure.   
- A data structure that can be serialized to disk and accessed via memory-mapping.   
- A data structure that can be updated while routeplanning is happening.

It contains stops, connections and trips. The stops can be retrieved, updated or deleted by their IDs and queried by their geographical location. The connections can be enumerated either by their departure or arrival time and can be retrieved, updated or deleted by their ID.

A high-level overview of how these things tie together:

![transit-db-diagram](docs/images/transit-db-lc-io-diagram.png)

A transit db can request new data from its connection source and a connection source can notify the transit db if it there is new data available.
