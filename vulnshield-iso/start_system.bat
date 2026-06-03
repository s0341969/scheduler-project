@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0start_system.ps1"

if errorlevel 1 (
    echo.
    echo Start failed. Check Docker and PowerShell output.
    pause
    exit /b 1
)

echo.
echo Startup script finished.
pause
