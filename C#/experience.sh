#!/bin/sh

id=$1

#touch temp
curl -s http://espn.go.com/mens-college-basketball/team/roster/_/id/$id/ > temp
cat temp | grep -o -n '>FR<' | wc -l > experience.txt
cat temp | grep -o -n '>SO<' | wc -l >> experience.txt
cat temp | grep -o -n '>JR<' | wc -l >> experience.txt
cat temp | grep -o -n '>SR<' | wc -l >> experience.txt
#rm temp



