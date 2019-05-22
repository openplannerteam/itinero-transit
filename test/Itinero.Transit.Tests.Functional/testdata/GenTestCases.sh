#! /bin/bash

idp="/home/pietervdvn/git/idp/src/IDP/bin/release/netcoreapp2.1/linux-x64/IDP"
echo $idp
$idp --ctosm https://www.openstreetmap.org/relation/9413958 $1T00:00 86400 --write-transit-db CentrumbusBrugge$1.transitdb

$idp --ctlc https://graph.irail.be/sncb/connections https://irail.be/stations/NMBS $1T00:00:00 86400 --write-transit-db fixed-test-cases-$1.transitdb


$idp --ctlc https://graph.irail.be/sncb/connections https://irail.be/stations/NMBS $1T00:00:00 86400 --write-transit-db fixed-test-cases-$1.transitdb


$idp --ctlc "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-wvl-$1.transitdb
$idp --ctlc "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/connections" "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ovl-$1.transitdb
$idp --ctlc "https://openplanner.ilabt.imec.be/delijn/Limburg/connections"         "https://openplanner.ilabt.imec.be/delijn/Limburg/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-lim-$1.transitdb
$idp --ctlc "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/connections" "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-vlb-$1.transitdb
$idp --ctlc "https://openplanner.ilabt.imec.be/delijn/Antwerpen/connections" "https://openplanner.ilabt.imec.be/delijn/Antwerpen/stops" $1T00:00 86400 --write-transit-db fixed-test-cases-de-lijn-ant-$1.transitdb
