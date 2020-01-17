#! /bin/bash

./GenTestCases $1 1day
dotnet run --read --create-vt pt/
scp -r pt/ staging:/var/services/vector-tiles-api/data/
