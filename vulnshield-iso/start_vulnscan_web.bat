@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

set "PROJECT_ROOT=%~dp0"
set "PROJECT_FILE=%PROJECT_ROOT%VulnScan.Web\VulnScan.Web.csproj"
set "APP_URL=http://localhost:5186/Auth/Login"
set "APP_TITLE=VulnScan.Web"

echo ===============================================
echo VulnScan.Web 一鍵啟動
echo ===============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [錯誤] 找不到 dotnet 指令，請先安裝 .NET SDK 10。
    pause
    exit /b 1
)

if not exist "%PROJECT_FILE%" (
    echo [錯誤] 找不到專案檔：
    echo %PROJECT_FILE%
    pause
    exit /b 1
)

echo [1/4] 建置 VulnScan.Web...
dotnet build "%PROJECT_ROOT%" >nul
if errorlevel 1 (
    echo.
    echo [錯誤] dotnet build 失敗，請先修正建置問題。
    pause
    exit /b 1
)

echo [2/4] 啟動 VulnScan.Web...
start "%APP_TITLE%" cmd /k "cd /d ""%PROJECT_ROOT%"" && dotnet run --project "".\VulnScan.Web\VulnScan.Web.csproj"" --launch-profile http --no-build"

echo [3/4] 等待網站就緒...
set "READY=0"
for /L %%I in (1,1,24) do (
    powershell.exe -NoProfile -Command ^
        "try { $r = Invoke-WebRequest -Uri '%APP_URL%' -MaximumRedirection 0 -ErrorAction Stop; if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 400) { exit 0 } else { exit 1 } } catch { if ($_.Exception.Response -and ($_.Exception.Response.StatusCode.value__ -ge 200) -and ($_.Exception.Response.StatusCode.value__ -lt 400)) { exit 0 } else { exit 1 } }"
    if not errorlevel 1 (
        set "READY=1"
        goto :ready
    )

    echo    等待中... %%I/24
    timeout /t 5 /nobreak >nul
)

:ready
if "%READY%"=="1" (
    echo [4/4] 開啟登入頁...
    start "" "%APP_URL%"
    echo.
    echo 啟動完成。
    echo 登入頁：%APP_URL%
    echo Development 預設帳號可參考：
    echo - admin / Admin123!Demo
    echo - secmgr / Security123!Demo
    echo - scanner / Scanner123!Demo
    echo - viewer / Viewer123!Demo
    echo.
    echo 關閉系統時，請直接關閉新開啟的 VulnScan.Web 視窗。
    pause
    exit /b 0
)

echo.
echo [錯誤] 已啟動應用程式，但 120 秒內尚未確認網站可用。
echo 請檢查新開啟的 VulnScan.Web 視窗輸出，常見原因為：
echo - MSSQL 連線字串不正確
echo - 5186 Port 已被占用
echo - appsettings 設定有誤
pause
exit /b 1
