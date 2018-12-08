# itinero-transit

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:anyways_Openplannerteam_ItineroTransit)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=anyways_Openplannerteam_ItineroTransit)  

This is a C# implementation of a client consuming [Linked Connections](https://linkedconnections.org/) to plan transit routes. This is going to replace the currently unfinished [transit module](https://github.com/itinero/transit) in [tinero](http://www.itinero.tech/).

We will try to simplify what was there by only storing connections and their stops. This way we can offer:

- Searching for stops by their name or geographically.
- Do CSA.

#### StopsDb

**Current:**

The current version is static and cannot be modified after the data has been loaded.

**New:**

We need a data structure that can be modified:

- Add new stops.
- Update stops.

We need to be able to query stops by:

- Their IDs.
- Their location.

Suggested:

- Store stops per tile.
- Keep an index per agency with IDs.

#### ConnectionsDb

**Current:**

A static database of sorted connections that cannot be modified after the data has been loaded.

**New:**

We need a data structure that supports:

- Adding new connections.
- Removing a connection. 

**Suggested:**

There are 1440 minutes in a day or 86400 seconds. We store connections per window of a # of seconds. This way we can enumerate them in a sorted manner and still add/remove connections. We can use the exact same idea when storing the inverse index. 

**Ideas:**

We can try to group connections together that have:

- Identical trip IDs.
- Identical departure time (excluding date).

