#!/bin/sh

read id

curl -s http://espn.go.com/mens-college-basketball/team/roster/_/id/$id/ > temp
cat temp | grep -o -n '>FR<' | wc -l
cat temp | grep -o -n '>SO<' | wc -l
cat temp | grep -o -n '>JR<' | wc -l
cat temp | grep -o -n '>SR<' | wc -l



