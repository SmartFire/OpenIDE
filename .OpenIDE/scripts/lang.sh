#!/bin/bash 

# Script parameters
#	Param 1: Script run location
#	Param 2: global profile name
#	Param 3: local profile name
#	Param 4-: Any passed argument
#
# When calling oi use the --profile=PROFILE_NAME and 
# --global-profile=PROFILE_NAME argument to ensure calling scripts
# with the right profile.
#
# To post back oi commands print command prefixed by command| to standard output
# To post a comment print to std output prefixed by comment|
# To post an error print to std output prefixed by error|

if [ "$2" = "get-command-definitions" ]; then
	# Definition format usually represented as a single line:

	# Script description|
	# command1|"Command1 description"
	# 	param|"Param description" end
	# end
	# command2|"Command2 description"
	# 	param|"Param description" end
	# end

	echo "Running language plugin|LANGUAGE|\"Language to run (cs,go)\" end"
	exit
fi

if [ "$4" = "cs" ]; then
	mono --debug Languages/CSharp/CSharp/bin/AutoTest.Net/C#.exe $1
fi

if [ "$4" = "go" ]; then
	Languages/go/src/go/go $1
fi

if [ "$4" = "js" ]; then
	node Languages/js/js.js $1
fi