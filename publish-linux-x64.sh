#!/usr/bin/env sh

rm -rdf packages/linux-x64/gridfs_server
mkdir -p packages/linux-x64/gridfs_server
dotnet publish src --force --output packages/linux-x64/gridfs_server -c Integration -r linux-x64 --no-self-contained
rm packages/linux-x64/gridfs_server/*.deps.json
