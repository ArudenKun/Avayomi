@echo off

set publishFolder="publish"
if exist %publishFolder% (
    echo Deleting %publishFolder%
    rd /s /q %publishFolder%
    echo %publishFolder% deleted.
) else (
    echo %publishFolder% does not exist.
)

set "appName=Avayomi"
set "proj=src\%appName%\%appName%.csproj"
if not exist "%proj%" (
  echo Could not find %proj%
  exit /b 1
)

setlocal enableextensions disabledelayedexpansion

echo.
echo Compiling %appName%
dotnet publish %proj% -c Release -o publish -p:PublishAot=false -p:PublishSingleFile=true --self-contained
pause