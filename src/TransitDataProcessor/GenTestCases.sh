#! /bin/bash

# dotnet run --ctosm https://www.openstreetmap.org/relation/9413958 $1T00:00 86400 --write-transit-db fixed-test-cases-osm-CentrumbusBrugge$1.transitdb

# dotnet run --ctlc https://graph.irail.be/sncb/connections https://irail.be/stations/NMBS $1T00:00:00 86400 --write-transit-db fixed-test-cases-sncb-$1.transitdb


echo "Loading all delijn datasets"
# dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Antwerpen/connections"       "https://openplanner.ilabt.imec.be/delijn/Antwerpen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ant-$1.transitdb

# dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Limburg/connections"         "https://openplanner.ilabt.imec.be/delijn/Limburg/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-lim-$1.transitdb

# dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ovl-$1.transitdb

# dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/connections"  "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-vlb-$1.transitdb

dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-wvl-$1.transitdb
