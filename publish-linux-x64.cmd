@echo off
rmdir /s /q packages\linux-x64\gridfs_server
mkdir packages\linux-x64\gridfs_server
dotnet publish src --force --output packages\linux-x64\gridfs_server -c Integration -r linux-x64 --no-self-contained
del packages\linux-x64\gridfs_server\*.deps.json packages\linux-x64\gridfs_server\*.endpoints.json
