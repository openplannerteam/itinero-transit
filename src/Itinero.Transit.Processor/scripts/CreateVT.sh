#! /bin/bash

./GenTestCases.sh $1 1day
cd ..
dotnet run --read --write-vt pt/
# scp -r pt/ staging:/var/services/vector-tiles-api/data/
