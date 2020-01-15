Itinero Transit Processor 
========================= 

The **Itinero Transit Processor** *(ITP)* helps to convert various public transport datasets into a transitdb which can be used to quickly solve routing queries.

Experimental switches are included in this document.
Usage
-----

The switches act as 'mini-programs' which are executed one after another.
A switch will either create, modify or write this data. This document details what switches are available.
In normal circumstances, only a single transit-db is loaded into memory.
However, ITP supports to have multiple transitDBs loaded at the same time if a read-switch is called multiple times.
Most modifying switches will execute their effect on all of them independently; but a few have a special effect if they are merged.
Most consuming switches will get a 'mashed-together'-version of all the databases.

Examples
--------

A few useful examples to get you started:

````
itp --read-gtfs gtfs.zip # read a gtfs archive
        --write-transit-db # write the data into a transitdb, so that we can routeplan with it
        --write-vector-tiles # And while we are at it, generate vector tiles from them as well
````


````
itp --read-transit-db data.transitdp # read a transitdb
        Itinero.Transit.Processor.Switch.Write.WriteStops stops.csv # Create a stops.csv of all the stop locations and information
        --validate # Afterwards, check the transitdb for issues
````

````
itp --read-transit-db data.transitdp # read a transitdb
        --select-time 2020-01-20T10:00:00 1hour # Select only connections departing between 10:00 and 11:00
        Itinero.Transit.Processor.Switch.Filter.SelectStopById http://some-agency.com/stop/123456 # Filter for this stop, and retain only connections and trips only using this stop
        --write-connections-to-csv # Write all the connections to console to inspect them
````

````
itp --read # read all the .transitdbs in the current directory
--shell # Open an interactive shell, in order to experiment with the data
````

Switch Syntax
-------------

The syntax of a switch is:

    --switch param1=value1 param2=value2
    # Or equivalent:
    --switch value1 value2

There is no need to explicitly give the parameter name, 
as long as *unnamed* parameters are in the same order as in the tables below. 
It doesn't mater if only some arguments, all arguments or even no arguments are named: 
`--switch value2 param1=value1`, `--switch value1 param2=value2` or `--switch param1=value1 value2` 
are valid just as well.

At last, `-param1` is a shorthand for `param=true`. This is useful for boolean flags.


Full overview of all options 
------------------------------- 

All switches are listed below. Click on a switch to get a full overview, including sub-arguments.

- [Reading data](#Reading-data)
  * [--read-linked-connections](#--read-linked-connections---read-lc---rlc) Creates a transit DB based on linked connections (or adds them to an already existing db).
  * [--read-open-street-map-relation](#--read-open-street-map-relation---read-osm---rosm) Creates a transit DB based on an OpenStreetMap-relation following the route scheme (or adds it to an already existing db).
  * [--read-gtfs](#--read-gtfs---rgtfs) Creates a transit DB based on GTFS (or adds them to an already existing db), for the explicitly specified timeframe
  * [--read-transit-db](#--read-transit-db---read-transit---read-tdb---rt---rtdb---read) Read a transitDB file as input to do all the data processing.
- [Filtering the transitdb](#Filtering-the-transitdb)
  * [--select-time](#--select-time---filter-time) Filters the transit-db so that only connections departing in the specified time window are kept.
  * [--select-bounding-box](#--select-bounding-box---bounding-box---bbox) Filters the transit-db so that only stops within the bounding box are kept.
  * [--select-stop](#--select-stop---select-stops---filter-stop---filter-stops) Filters the transit-db so that only stops with the given id(s) are kept.
  * [--select-trip](#--select-trip---filter-trip) Removes all connections and all stops form the database, except those of the specified trip 
- [Validating and testing the transitdb](#Validating-and-testing-the-transitdb)
  * [--validate](#--validate) Checks assumptions on the database, e.
  * [--undo-delays](#--undo-delays---japanize---the-dutch-are-better---swiss-perfection) Removes all the delays of the trips, so recreate the planned schedule.
  * [--remove-unused](#--remove-unused---filter-unused---rm-unused) Removes stops and trips without connections.
  * [--show-info](#--show-info---info) Dumps all the metadata of the currently loaded database
- [Writing to file and to other formats](#Writing-to-file-and-to-other-formats)
  * [--write-transit-db](#--write-transit-db---write-transitdb---write-transit---write---wt) Write a transitDB to disk
  * [--write-vector-tiles](#--write-vector-tiles---write-vt---vt) Creates a vector tile representation of the loaded transitDb
  * [--write-stops](#--write-stops) Writes all stops contained in a transitDB to console
  * [--write-connections-to-csv](#--write-connections-to-csv---write-connections) Writes all connections contained in a transitDB to console
  * [--write-routes](#--write-routes---routes) Create an overview of routes and shows them.
  * [--write-trips](#--write-trips) Writes all trips contained in a transitDB to console or file
- [Misc](#Misc)
  * [--help](#--help---?---h) Print the help message
  * [--shell](#--shell---interactive---i) Starts an interactive shell where switches can be used as commands
  * [--clear](#--clear) Removes the currently loaded database from memory.
  * [--garbage-collect](#--garbage-collect---gc) Run garbage collection.
### Reading data

#### --read-linked-connections (--read-lc, --rlc)

This switch is a transitdb-source

   Creates a transit DB based on linked connections (or adds them to an already existing db). For this, the linked connections source and a timewindow should be specified.
If the previous switch reads or creates a transit db as well, the two transitDbs are merged into a single one.

Note that this switch only downloads the connections and keeps them in memory. To write them to disk, add --write-transit-db too.

Example usage to create the database for the Belgian Railway (SNCB/NMBS):

        --read-lc https://graph.irail.be/sncb/connections https://irail.be/stations/NMBS

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **connections, curl** | _Obligated param_ | The URL where connections can be downloaded. Special value: 'nmbs' | 
| **locations, stops, lurl** | _Obligated param_ | The URL where the location can be downloaded. Special value: 'nmbs' | 
| window-start, start | `now`| The start of the timewindow to load. Specify 'now' to take the current date and time. Otherwise provide a timestring of the format 'YYYY-MM-DDThh:mm:ss' (where T is a literal T). Special values: 'now' and 'today' | 
| window-duration, duration | `3600`| The length of the window to load, in seconds. If zero is specified, no connections will be downloaded. Special values: 'xhour', 'xday' | 

#### --read-open-street-map-relation (--read-osm, --rosm)

This switch is a transitdb-source

   Creates a transit DB based on an OpenStreetMap-relation following the route scheme (or adds it to an already existing db). For all information on Public Transport tagging, refer to [the OSM-Wiki](https://wiki.openstreetmap.org/wiki/Public_transport).n
A timewindow should be specified to indicate what period the transitDB should cover. 

Of course, the relation itself should be provided. Either:

 - Pass the ID of the relation to download it
 - Pass the URL of a relation.xml
 - Pass the filename of a relation.xml

If the previous switch reads or creates a transit db as well, the two transitDbs are merged into a single one.

Note that this switch only downloads/reads the relation and keeps them in memory. To write them to disk, add --write-transit-db too.

Example usage to create the database:

        idp --create-transit-osm 9413958

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **relation, id** | _Obligated param_ | Either a number, an url (starting with http or https) or a path where the relation can be found | 
| window-start, start | `now`| The start of the timewindow to load. Specify 'now' to take the current date and time. | 
| window-duration, duration | `3600`| The length of the window to load, in seconds. If zero is specified, no connections will be downloaded. | 

#### --read-gtfs (--rgtfs)

This switch is a transitdb-source

   Creates a transit DB based on GTFS (or adds them to an already existing db), for the explicitly specified timeframe

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **path** | _Obligated param_ | The path of the GTFS archive | 
| window-start, start | `now`| The start of the timewindow to load. Specify 'now' to take the current date and time. Otherwise provide a timestring of the format 'YYYY-MM-DDThh:mm:ss' (where T is a literal T). Special values: 'now' and 'today' | 
| window-duration, duration | `3600`| The length of the window to load, in seconds. If zero is specified, no connections will be downloaded. Special values: 'xhour', 'xday' | 

#### --read-transit-db (--read-transit, --read-tdb, --rt, --rtdb, --read)

This switch is multi-compatible, a transitdb-source

   Read a transitDB file as input to do all the data processing. A transitDB is a database containing connections between multiple stops

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| file | `*.transitdb`| The input file(s) to read, ',' seperated | 

### Filtering the transitdb

#### --select-time (--filter-time)

This switch is a transitdb-modifier

   Filters the transit-db so that only connections departing in the specified time window are kept. This allows to take a small slice out of the transitDB, which can be useful to debug. Only used locations will be kept.

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **window-start, start** | _Obligated param_ | The start time of the window, specified as `YYYY-MM-DD_hh:mm:ss` (e.g. `2019-12-31_23:59:59`) | 
| **duration, window-end** | _Obligated param_ | Either the length of the time window in seconds or the end of the time window in `YYYY-MM-DD_hh:mm:ss` | 
| allow-empty | `false`| If flagged, the program will not crash if no connections are retained | 

#### --select-bounding-box (--bounding-box, --bbox)

This switch is a transitdb-modifier

   Filters the transit-db so that only stops within the bounding box are kept. All connections containing a removed location will be removed as well.

This switch is mainly used for debugging.

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **left** | _Obligated param_ | Specifies the minimal latitude of the output. | 
| **right** | _Obligated param_ | Specifies the maximal latitude of the output. | 
| **top, up** | _Obligated param_ | Specifies the minimal longitude of the output. | 
| **bottom, down** | _Obligated param_ | Specifies the maximal longitude of the output. | 
| allow-empty | `false`| If flagged, the program will not crash if no stops are retained | 
| allow-empty-connections | `false`| If flagged, the program will not crash if no connections are retained | 

#### --select-stop (--select-stops, --filter-stop, --filter-stops)

This switch is a transitdb-modifier

   Filters the transit-db so that only stops with the given id(s) are kept. All connections containing a removed location will be removed as well.

This switch is mainly used for fancy statistics.

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **id, ids** | _Obligated param_ | The ';'-separated stops that should be kept | 
| allow-empty-connections | `false`| If flagged, the program will not crash if no connections are retained | 

#### --select-trip (--filter-trip)

This switch is a transitdb-modifier

   Removes all connections and all stops form the database, except those of the specified trip 

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| **id** | _Obligated param_ | The URI identifying the trip you want to keep | 

### Validating and testing the transitdb

#### --validate

This switch is a transitdb-sink

   Checks assumptions on the database, e.g: are the coordinates of stops within the correct range? Does the train not drive impossibly fast? Are there connections going back in time?

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| type | `*`| Only show messages of this type. Multiple are allowed if comma-separated. Note: the totals will still be printed | 
| cutoff | `10`| Only show this many messages. Default: 25 | 
| relax | `false`| Use more relaxed parameters for real-world data, if they should not be a problem for journey planning. For example, teleportations <10km are ignored, very fast trains <10km are ignored. Notice that I would expect those to cases to cause regular delays though! | 

#### --undo-delays (--japanize, --the-dutch-are-better, --swiss-perfection)

This switch is a transitdb-modifier

   Removes all the delays of the trips, so recreate the planned schedule.



*This switch does not need parameters*

#### --remove-unused (--filter-unused, --rm-unused)

This switch is **experimental**, a transitdb-modifier

   Removes stops and trips without connections.



*This switch does not need parameters*

#### --show-info (--info)

This switch is **experimental**, multi-compatible, a transitdb-sink

   Dumps all the metadata of the currently loaded database



*This switch does not need parameters*

### Writing to file and to other formats

#### --write-transit-db (--write-transitdb, --write-transit, --write, --wt)

This switch is multi-compatible, a transitdb-sink

   Write a transitDB to disk

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| file | `$operatorName.YYYY-mm-dd.transitdb`| The output file to write to | 

#### --write-vector-tiles (--write-vt, --vt)

This switch is multi-compatible, a transitdb-sink

   Creates a vector tile representation of the loaded transitDb

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| directory | `vector-tiles`| The directory to write the data to | 
| minzoom | `3`| The minimal zoom level that this vector tiles are generated for | 
| maxzoom | `14`| The maximal zoom level that the vector tiles are generated for. Note: maxzoom should be pretty big, as lines sometimes disappear if they have no point in a tile | 

#### --write-stops

This switch is a transitdb-sink

   Writes all stops contained in a transitDB to console

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| file | _NA_| The file to write the data to, in .csv format | 

#### --write-connections-to-csv (--write-connections)

This switch is a transitdb-sink

   Writes all connections contained in a transitDB to console

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| file | _NA_| The file to write the data to, in .csv format | 
| human | `false`| Use less exact but more human-friendly output | 

#### --write-routes (--routes)

This switch is **experimental**, a transitdb-sink

   Create an overview of routes and shows them. A route is a list of stops, where at least one trip does all of them in order



*This switch does not need parameters*

#### --write-trips

This switch is a transitdb-sink

   Writes all trips contained in a transitDB to console or file

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| file | _NA_| The file to write the data to, in .csv format | 

### Misc

#### --help (--?, --h)

This switch is multi-compatible, a transitdb-source, a transitdb-sink

   Print the help message

| Parameter  | Default value | Explanation       |
|----------- | ------------- | ----------------- |
| about | _NA_| The command (or switch) you'd like more info about | 
| markdown, md | _NA_| Write the help text as markdown to a file. The documentation is generated with this flag. | 
| experimental | `false`| Include experimental switches in the output | 
| short | `false`| Only print a small overview | 

#### --shell (--interactive, --i)

This switch is multi-compatible, a transitdb-source, a transitdb-modifier, a transitdb-sink

   Starts an interactive shell where switches can be used as commands



*This switch does not need parameters*

#### --clear

This switch is multi-compatible, a transitdb-modifier

   Removes the currently loaded database from memory. This switch is only useful in interactive shell sessions



*This switch does not need parameters*

#### --garbage-collect (--gc)

This switch is **experimental**, multi-compatible, a transitdb-source, a transitdb-modifier, a transitdb-sink

   Run garbage collection. This is for debugging



*This switch does not need parameters*

