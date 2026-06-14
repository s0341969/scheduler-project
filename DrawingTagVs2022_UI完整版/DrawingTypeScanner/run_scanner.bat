@echo off
setlocal
title DrawingTypeScanner Runner

set "ROOT=%~dp0"
set "APPDIR=%ROOT%bin\Release\net8.0\win-x64"
set "EXE=%APPDIR%\DrawingTypeScanner.exe"
set "CONFIG=%APPDIR%\appsettings.json"

echo ====================================================
echo   DrawingTypeScanner One-Click Runner
echo ====================================================
echo   AppDir : %APPDIR%
echo   Exe    : %EXE%
echo   Config : %CONFIG%
echo.

if not exist "%APPDIR%" goto :missing_appdir
if not exist "%EXE%" goto :missing_exe
if not exist "%CONFIG%" goto :missing_config

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$cfg = Get-Content -LiteralPath '%CONFIG%' -Raw | ConvertFrom-Json; $conn = [string]$cfg.ConnectionStrings.DefaultConnection; $root = [string]$cfg.Scanner.RootPath; $pattern = [string]$cfg.Scanner.SearchPattern; $recursive = [string]$cfg.Scanner.EnableRecursiveScan; $parallel = [string]$cfg.Scanner.MaxDegreeOfParallelism; $batch = [string]$cfg.Scanner.WriteBatchSize; $timeout = [string]$cfg.Scanner.CommandTimeoutSeconds; $safeConn = [System.Text.RegularExpressions.Regex]::Replace($conn, '(?i)(Password|Pwd)\s*=\s*[^;]*', '$1=***'); Write-Output ('  ScanRoot   : ' + $root); Write-Output ('  Pattern    : ' + $pattern); Write-Output ('  Recursive  : ' + $recursive); Write-Output ('  Parallel   : ' + $parallel); Write-Output ('  BatchSize  : ' + $batch); Write-Output ('  SqlTimeout : ' + $timeout + ' sec'); Write-Output ('  DB         : ' + $safeConn)"`) do echo %%I
echo.

echo [1/1] Running DrawingTypeScanner...
echo       This mode runs in foreground and keeps the console visible.
echo       Press Ctrl+C to stop.
echo.

pushd "%APPDIR%"
DrawingTypeScanner.exe
set "EXITCODE=%ERRORLEVEL%"
popd

echo.
echo DrawingTypeScanner exited with code %EXITCODE%.
echo.
pause
exit /b %EXITCODE%

:missing_appdir
echo [ERROR] App folder not found:
echo %APPDIR%
echo.
pause
exit /b 1

:missing_exe
echo [ERROR] Executable not found:
echo %EXE%
echo.
pause
exit /b 1

:missing_config
echo [ERROR] appsettings.json not found:
echo %CONFIG%
echo.
pause
exit /b 1
