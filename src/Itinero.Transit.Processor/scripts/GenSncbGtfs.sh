#! /bin/bash

GTFS="../../test/Itinero.Transit.Tests/IO/GTFS/sncb-13-october.zip"
dotnet run --rgtfs $GTFS 2019-10-13+2 1day --write-transit-db sncb.2019-10-13.transitdb
