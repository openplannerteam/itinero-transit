#! /bin/bash

# A script to show the last log
# If you see this script as it's own output, there are no logs (or their name is < 'last.sh')

LAST=`ls | tail -n 1`
cat "$LAST" | sed "s/\\\\n/\\n|   /g" | sed "s/{\"Timestamp\":\"/\n### /g" | sed "s/\",\"Level\":\"Information\",\"MessageTemplate\":/\n|   /g"

