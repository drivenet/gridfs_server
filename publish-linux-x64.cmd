@echo off
rmdir /s /q src\bin\Release\netcoreapp2.2\linux-x64\publish
dotnet publish -c Release -r linux-x64 --self-contained false
rmdir /s /q packages\gridfs_server-linux-x64
mkdir packages\gridfs_server-linux-x64
xcopy src\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.dll packages\gridfs_server-linux-x64
xcopy src\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.pdb packages\gridfs_server-linux-x64
xcopy src\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.so packages\gridfs_server-linux-x64
xcopy src\bin\x64\Release\netcoreapp2.2\linux-x64\publish\*.runtimeconfig.json packages\gridfs_server-linux-x64
