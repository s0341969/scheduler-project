# RemoteDesk Control

這個專案是一套以銷售與產品化為目標的遠端桌面基礎模組，採用 `Control Server + Windows Agent` 雙專案架構，而不是單機型 VNC。這樣做的目的，是把「速度、部署、管理」拆成獨立責任：

- `Control Server`
  負責登入、裝置管理、WebSocket relay、MSSQL 狀態與上線審計、後台頁面。
- `Windows Agent`
  部署在受控 Windows 主機，負責桌面擷取、畫面壓縮、回送心跳、接收輸入並執行。

## 目前能力

- 單一 Control Server 可管理多台已註冊 Agent
- Agent 以二進位 WebSocket 直接傳送 JPEG frame，減少伺服器中繼成本
- 後台登入與裝置清單管理
- 單一裝置單一控制者模式，避免多人同控衝突
- 滑鼠移動、點擊、右鍵、滾輪、常見按鍵與文字輸入
- MSSQL 紀錄裝置上線時間、最後心跳、離線原因
- 根目錄可直接執行 `dotnet build`

## 專案結構

- `RemoteDesktopSystem.csproj`
  根目錄聚合建置專案，讓 `dotnet build` 一次建兩個子專案。
- `src/RemoteDesktop.Host`
  ASP.NET Core 控制台。
- `src/RemoteDesktop.Agent`
  Windows Agent。
- `src/RemoteDesktop.Host/DatabaseScripts/001_create_remote_desktop_schema.sql`
  MSSQL 初始化腳本。

## 為什麼這版比單純 VNC 更適合賣

- 控制面與被控端分離，後續才能做多租戶、裝置群組、授權與計費。
- 影像中繼不經資料庫，Server 只做 WebSocket relay，延遲路徑短。
- Agent 端先壓縮再傳，方便後續切換成更高效 codec 或增量更新。
- 裝置在線資訊與上線歷史進 MSSQL，便於營運、稽核與 SLA 報表。
- 根目錄可直接 build，部署時不必先理解多專案 solution restore 問題。

## 需求

- Windows 10/11
- .NET 8 SDK
- MSSQL 或 LocalDB
- 受控端需有互動式 Windows 桌面工作階段

## 設定

### Control Server

檔案：`src/RemoteDesktop.Host/appsettings.json`

```json
{
  "ConnectionStrings": {
    "RemoteDesktopDb": "Server=(localdb)\\MSSQLLocalDB;Database=RemoteDesktopControl;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "ControlServer": {
    "ConsoleName": "RemoteDesk Control",
    "AdminUserName": "admin",
    "AdminPassword": "ChangeMe!2026",
    "RequireHttpsRedirect": false,
    "SharedAccessKey": "ChangeMe-Agent-Key",
    "AgentHeartbeatTimeoutSeconds": 45
  }
}
```

### Windows Agent

檔案：`src/RemoteDesktop.Agent/appsettings.json`

```json
{
  "Agent": {
    "ServerUrl": "http://localhost:5000",
    "DeviceId": "device-demo-001",
    "DeviceName": "Demo Operator PC",
    "SharedAccessKey": "ChangeMe-Agent-Key",
    "CaptureFramesPerSecond": 8,
    "JpegQuality": 55,
    "MaxFrameWidth": 1600,
    "ReconnectDelaySeconds": 5
  }
}
```

`SharedAccessKey` 必須與 Server 相同。

## 建置

在專案根目錄執行：

```powershell
$env:DOTNET_CLI_HOME="$PWD\\.dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT="1"
& 'C:\Program Files\dotnet\dotnet.exe' build
```

## 執行

### 啟動 Control Server

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --project .\src\RemoteDesktop.Host\RemoteDesktop.Host.csproj
```

### 啟動 Windows Agent

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --project .\src\RemoteDesktop.Agent\RemoteDesktop.Agent.csproj
```

## MSSQL 表

- `dbo.RemoteDesktopDevices`
  保存每台裝置目前狀態與最後上線資訊
- `dbo.RemoteDesktopAgentPresenceLogs`
  保存每次 Agent 上線區間與離線原因

## 部署與管理建議

- 這版適合先作為內網版商用 MVP。
- 正式對外銷售前，建議補上：
  - 每裝置獨立憑證或註冊金鑰，不要只用全域共用金鑰
  - HTTPS 與反向代理
  - 多使用者 RBAC
  - 壓縮協定升級成差分畫面或硬體編碼
  - 安裝器、Windows Service、靜默部署腳本
