@echo off
setlocal EnableExtensions

set "PROJECT_ROOT=%~dp0"
set "PROJECT_FILE=%PROJECT_ROOT%VulnScan.Web\VulnScan.Web.csproj"
set "BUILD_DLL=%PROJECT_ROOT%VulnScan.Web\bin\Debug\net10.0\VulnScan.Web.dll"
set "APP_URL=http://localhost:5186/Auth/Login"
set "APP_TITLE=VulnScan.Web"

echo ===============================================
echo VulnScan.Web One-Click Startup
echo ===============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet command not found. Install .NET SDK 10 first.
    pause
    exit /b 1
)

if not exist "%PROJECT_FILE%" (
    echo [ERROR] Project file not found:
    echo %PROJECT_FILE%
    pause
    exit /b 1
)

if not exist "%PROJECT_ROOT%VulnScan.Web\App_Data" (
    mkdir "%PROJECT_ROOT%VulnScan.Web\App_Data" >nul 2>nul
)
if not exist "%PROJECT_ROOT%VulnScan.Web\App_Data\AutoImport\Nuclei\incoming" (
    mkdir "%PROJECT_ROOT%VulnScan.Web\App_Data\AutoImport\Nuclei\incoming" >nul 2>nul
)
if not exist "%PROJECT_ROOT%VulnScan.Web\App_Data\AutoImport\Nessus\incoming" (
    mkdir "%PROJECT_ROOT%VulnScan.Web\App_Data\AutoImport\Nessus\incoming" >nul 2>nul
)

echo [1/4] Building VulnScan.Web...
dotnet build "%PROJECT_FILE%" --no-restore
if errorlevel 1 (
    echo Fast build failed. Trying full restore + build...
    dotnet build "%PROJECT_FILE%"
    if errorlevel 1 (
        if exist "%BUILD_DLL%" (
            echo Build failed, but existing build output was found:
            echo %BUILD_DLL%
            echo Starting with the previous build output...
        ) else (
            echo.
            echo [ERROR] dotnet build failed and no previous build output was found.
            pause
            exit /b 1
        )
    )
)

echo [2/4] Starting VulnScan.Web...
start "%APP_TITLE%" cmd /k "cd /d ""%PROJECT_ROOT%"" && dotnet run --project "".\VulnScan.Web\VulnScan.Web.csproj"" --launch-profile http --no-build --no-restore"

echo [3/4] Waiting for application startup...
set "READY=0"
for /L %%I in (1,1,24) do (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "try { $response = Invoke-WebRequest -Uri '%APP_URL%' -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop; exit 0 } catch { exit 1 }"
    if not errorlevel 1 (
        set "READY=1"
        goto ready
    )

    echo Waiting... %%I/24
    ping 127.0.0.1 -n 6 >nul
)

:ready
if "%READY%"=="1" (
    echo [4/4] Opening login page...
    start "" "%APP_URL%"
    echo.
    echo Startup completed.
    echo Login URL: %APP_URL%
    echo Development sample accounts:
    echo - admin / Admin123!Demo
    echo - secmgr / Security123!Demo
    echo - scanner / Scanner123!Demo
    echo - viewer / Viewer123!Demo
    echo.
    echo To stop the app, close the new VulnScan.Web console window.
    pause
    exit /b 0
)

echo.
echo [ERROR] The app process started, but the login page did not respond within 120 seconds.
echo Check the VulnScan.Web console window for details.
echo Common causes:
echo - invalid SQLite or MSSQL connection string
echo - database file path is not writable
echo - port 5186 already in use
echo - invalid appsettings configuration
pause
exit /b 1
