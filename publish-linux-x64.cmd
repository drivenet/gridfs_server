@echo off
rmdir /s /q packages\linux-x64\gridfs_server
mkdir packages\linux-x64\gridfs_server
dotnet publish src --force --output packages\linux-x64\gridfs_server -c Integration -r linux-x64 --self-contained false
move packages\linux-x64\gridfs_server\Microsoft.Extensions.Hosting.Systemd.dll "%TEMP%"
move packages\linux-x64\gridfs_server\Microsoft.AspNetCore.Buffering.dll "%TEMP%"
del packages\linux-x64\gridfs_server\web.config packages\linux-x64\gridfs_server\*.deps.json packages\linux-x64\gridfs_server\Microsoft.*.dll
move "%TEMP%\Microsoft.Extensions.Hosting.Systemd.dll" packages\linux-x64\gridfs_server
move "%TEMP%\Microsoft.AspNetCore.Buffering.dll" packages\linux-x64\gridfs_server
