@echo off
rmdir /s /q src\bin\Release\netcoreapp2.1\publish
dotnet publish -c Release -f netcoreapp2.1
rmdir /s /q packages\gridfs_server-linux-x64
mkdir packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.1\publish\*.dll packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.1\publish\*.pdb packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.1\publish\*.runtimeconfig.json packages\gridfs_server-linux-x64
xcopy src\bin\Release\netcoreapp2.1\publish\runtimes\linux-x64\native\*.so packages\gridfs_server-linux-x64