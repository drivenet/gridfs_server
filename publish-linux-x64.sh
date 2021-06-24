#!/usr/bin/env sh

rm -rdf packages/linux-x64/gridfs_server
mkdir -p packages/linux-x64/gridfs_server

dotnet publish src --force --output packages/linux-x64/gridfs_server -c Integration -r linux-x64 --self-contained false

mv packages/linux-x64/gridfs_server/Microsoft.Extensions.Hosting.Systemd.dll /tmp
mv packages/linux-x64/gridfs_server/Microsoft.AspNetCore.Buffering.dll /tmp
rm packages/linux-x64/gridfs_server/web.config packages/linux-x64/gridfs_server/*.deps.json packages/linux-x64/gridfs_server/Microsoft.*.dll
mv "/tmp/Microsoft.Extensions.Hosting.Systemd.dll" packages/linux-x64/gridfs_server
mv "/tmp/Microsoft.AspNetCore.Buffering.dll" packages/linux-x64/gridfs_server

