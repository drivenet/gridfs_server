@echo off
rmdir /s /q packages\linux-x64\gridfs_server
mkdir packages\linux-x64\gridfs_server
dotnet publish src --output packages\linux-x64\gridfs_server -c Release -r linux-x64 --self-contained false
rmdir /s /q packages\linux-x64\gridfs_server\refs
del packages\linux-x64\gridfs_server\gridfs_server packages\linux-x64\gridfs_server\web.config packages\linux-x64\gridfs_server\*.deps.json
