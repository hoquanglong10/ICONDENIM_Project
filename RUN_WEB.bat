@echo off
cd /d %~dp0\ICONDENIM.Web
dotnet restore
dotnet run
pause
