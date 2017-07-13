@echo off
rmdir /s /q src\bin\Release\netcoreapp2.0\publish
dotnet publish -c Release -f netcoreapp2.0
rmdir /s /q packages\gridfs_server-linux-x64
mkdir packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.0\publish\*.dll packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.0\publish\*.pdb packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.0\publish\*.runtimeconfig.json packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.0\publish\runtimes\linux-x64\native\*.so packages\gridfs_server-linux-x64