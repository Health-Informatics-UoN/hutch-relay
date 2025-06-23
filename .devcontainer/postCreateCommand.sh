#! /bin/sh

# aggressively run migrations
dotnet run --project app/Hutch.Relay -- database update

# create a user/subnode immediately; check logs for password / collection id
# This may fail if `test` user already exists, but that's not a problem
dotnet run --project app/Hutch.Relay -- users add test

# sadly if the user does exist, devs will need to exec in to reset the password if they don't know it
# since we don't want to reset it aggressively here

# we can log out subnode ids for the user though:
dotnet run --project app/Hutch.Relay -- users list-subnodes test