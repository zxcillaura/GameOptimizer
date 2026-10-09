@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem Use the requested SDK location when it exists.
if exist "E:\Programs\dotnet\dotnet.exe" set "DOTNET=E:\Programs\dotnet\dotnet.exe"
if not defined DOTNET set "DOTNET=dotnet"

"%DOTNET%" --info
if errorlevel 1 (
  echo .NET SDK was not found. Install .NET 8 SDK or set PATH manually.
  exit /b 1
)

echo.
echo === Building GAMEOPTIMIZ 1.0 (Release) ===
"%DOTNET%" build -c Release
if errorlevel 1 exit /b 1

echo.
echo === Publishing single-file GAMEOPTIMIZ1.0.exe ===
"%DOTNET%" publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
if errorlevel 1 exit /b 1

echo.
echo Build completed: bin\Release\net8.0-windows\win-x64\publish\GAMEOPTIMIZ1.0.exe
exit /b 0
