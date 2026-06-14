@echo off
setlocal
title DrawingTagWeb Latest Runner

set "ROOT=%~dp0"
set "APPROOT=%ROOT%publish_output"
set "RUNTIMEROOT=%ROOT%DrawingTagWeb\bin\Release\net8.0\win-x64"
set "APPDLL=%APPROOT%\DrawingTagWeb.dll"
set "RUNTIMEEXE=%RUNTIMEROOT%\DrawingTagWeb.exe"
set "CONFIG=%APPROOT%\appsettings.json"
set "URL=http://10.1.1.12:5088"
set "VERSION=V2026.05.16.05"
set "RUNROOT=%TEMP%\DrawingTagWeb_latest_runner"
set "RUNEXE=%RUNROOT%\DrawingTagWeb.exe"

echo ====================================================
echo   DrawingTagWeb Latest Runner
echo ====================================================
echo   Version: %VERSION%
echo   App    : %APPROOT%
echo   Runtime: %RUNTIMEROOT%
echo   RunDir : %RUNROOT%
echo   Url    : %URL%
echo.

where dotnet >nul 2>nul
if errorlevel 1 goto :no_dotnet
if not exist "%APPROOT%" goto :missing_app
if not exist "%RUNTIMEROOT%" goto :missing_runtime
if not exist "%APPDLL%" goto :missing_app_dll
if not exist "%RUNTIMEEXE%" goto :missing_runtime_exe
if not exist "%CONFIG%" goto :missing_config

if exist "%RUNROOT%" rmdir /s /q "%RUNROOT%"
mkdir "%RUNROOT%" >nul 2>nul
if errorlevel 1 goto :prepare_failed

echo [1/3] Preparing ASCII-only runtime folder...
xcopy "%RUNTIMEROOT%\*" "%RUNROOT%\" /E /I /Y /Q >nul
if errorlevel 1 goto :copy_runtime_failed

echo [2/3] Overlaying latest published app files...
xcopy "%APPROOT%\*" "%RUNROOT%\" /E /I /Y /Q >nul
if errorlevel 1 goto :copy_app_failed

if not exist "%RUNEXE%" goto :missing_run_exe

set "RUNNATIVE=%RUNROOT%\runtimes\win-x64\native"
set "RUNX64=%RUNROOT%\x64"
set "RUNNATIVE86=%RUNROOT%\runtimes\win-x86\native"
set "RUNX86=%RUNROOT%\x86"
if exist "%RUNNATIVE%" set "PATH=%RUNNATIVE%;%PATH%"
if exist "%RUNX64%" set "PATH=%RUNX64%;%PATH%"
if exist "%RUNNATIVE86%" set "PATH=%RUNNATIVE86%;%PATH%"
if exist "%RUNX86%" set "PATH=%RUNX86%;%PATH%"

echo [3/3] Running latest published DrawingTagWeb...
echo       This mode runs in foreground and keeps the console visible.
echo       Press Ctrl+C to stop the website.
echo       Open browser to: %URL%
echo.

pushd "%RUNROOT%"
DrawingTagWeb.exe
set "EXITCODE=%ERRORLEVEL%"
popd

echo.
echo DrawingTagWeb exited with code %EXITCODE%.
echo.
pause
exit /b %EXITCODE%

:prepare_failed
echo [ERROR] Failed to prepare runner folder:
echo %RUNROOT%
echo.
pause
exit /b 1

:copy_runtime_failed
echo [ERROR] Failed to copy runtime files to runner folder.
echo Source: %RUNTIMEROOT%
echo Target: %RUNROOT%
echo.
pause
exit /b 1

:copy_app_failed
echo [ERROR] Failed to overlay latest app files to runner folder.
echo Source: %APPROOT%
echo Target: %RUNROOT%
echo.
pause
exit /b 1

:no_dotnet
echo [ERROR] dotnet is not installed or not in PATH.
echo Please install .NET 8 runtime or SDK first.
echo.
pause
exit /b 1

:missing_app
echo [ERROR] Latest publish_output folder not found:
echo %APPROOT%
echo.
pause
exit /b 1

:missing_runtime
echo [ERROR] Runtime source folder not found:
echo %RUNTIMEROOT%
echo.
pause
exit /b 1

:missing_app_dll
echo [ERROR] Latest app DLL not found:
echo %APPDLL%
echo.
pause
exit /b 1

:missing_runtime_exe
echo [ERROR] Runtime executable not found:
echo %RUNTIMEEXE%
echo.
pause
exit /b 1

:missing_config
echo [ERROR] appsettings.json not found:
echo %CONFIG%
echo.
pause
exit /b 1

:missing_run_exe
echo [ERROR] Runner executable was not created:
echo %RUNEXE%
echo.
pause
exit /b 1
