@echo off
rmdir /s /q packages\linux-x64\gridfs_server
mkdir packages\linux-x64\gridfs_server
dotnet publish src --output packages\linux-x64\gridfs_server -c Release -r linux-x64 --self-contained false
move packages\linux-x64\gridfs_server\Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.dll "%TEMP%"
del packages\linux-x64\gridfs_server\gridfs_server packages\linux-x64\gridfs_server\web.config packages\linux-x64\gridfs_server\*.deps.json packages\linux-x64\gridfs_server\*settings.json packages\linux-x64\gridfs_server\Microsoft.*.dll
move "%TEMP%\Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.dll" packages\linux-x64\gridfs_server
