@echo off

set releaseFolder="release"
set publishFolder="publish"
if exist %releaseFolder% (
    echo Deleting %releaseFolder%
    rd /s /q %releaseFolder%
    echo %releaseFolder% deleted.
) else (
    echo %releaseFolder% does not exist.
)
if exist %publishFolder% (
    echo Deleting %publishFolder%
    rd /s /q %publishFolder%
    echo %publishFolder% deleted.
) else (
    echo %publishFolder% does not exist.
)

set "appName=Avayomi"
set "proj=src\Desktop\Desktop.csproj"
set "props=Directory.Build.props"
if not exist "%proj%" (
  echo Could not find %proj%
  exit /b 1
)

setlocal enableextensions disabledelayedexpansion
set "version="
if not defined version (
  for /f "tokens=3 delims=<>" %%a in (
      'find /i "<Version>" ^< "%props%"'
  ) do set "version=%%a"
  if not defined version (
    echo Version is missing from %props% file.
    exit /b 1
  )
)

echo.
echo Compiling %appName%
dotnet publish %proj% -c Release -o publish
echo.
echo Building Velopack Release v%version%
vpk pack -u %appName% -e Desktop.exe -o release -p publish -v %version%-dev.1 -c win-x64-dev
pause