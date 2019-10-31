#! /bin/bash

DATE="$1"

dotnet run --ctosm https://www.openstreetmap.org/relation/9413958 "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-osm-CentrumbusBrugge-"$DATE".transitdb

dotnet run --ctlc https://graph.irail.be/sncb/connections https://graph.irail.be/sncb/stops "$DATE"T00:00:00 86400 --sncb-filter --write-transit-db fixed-test-cases-sncb-"$DATE".transitdb


echo "Loading all delijn datasets"
dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Antwerpen/connections"     "https://openplanner.ilabt.imec.be/delijn/Antwerpen/stops" "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ant-"$DATE".transitdb

dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Limburg/connections"         "https://openplanner.ilabt.imec.be/delijn/Limburg/stops" "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-lim-"$DATE".transitdb

dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/stops" "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ovl-"$DATE".transitdb

dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/connections"  "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/stops" "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-vlb-"$DATE".transitdb

dotnet run --ctlc "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/stops" "$DATE"T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-wvl-"$DATE".transitdb
