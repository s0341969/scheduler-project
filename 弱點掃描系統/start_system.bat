@echo off
setlocal
cd /d "%~dp0"

powershell -ExecutionPolicy Bypass -File "%~dp0start_system.ps1"

if errorlevel 1 (
    echo.
    echo 系統啟動失敗，請檢查 Docker 與 PowerShell 輸出訊息。
    pause
    exit /b 1
)

echo.
echo 系統啟動腳本已執行完成。
pause
