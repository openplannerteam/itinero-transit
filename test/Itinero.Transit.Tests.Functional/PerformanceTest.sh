#!/usr/bin/env bash
perf record -g dotnet run -c release
perf script | ~/git/FlameGraph/stackcollapse-perf.pl | ~/git/FlameGraph/flamegraph.pl > flame.svg && o flame.svg &
perf report -g

