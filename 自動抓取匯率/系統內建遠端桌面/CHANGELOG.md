# Changelog

## 2026-04-07

- 將空專案重構為產品型遠端桌面架構：`Control Server + Windows Agent`
- 新增 ASP.NET Core 控制台，包含登入、裝置清單、遠端控制頁、上線紀錄頁
- 新增 Agent WebSocket 註冊、心跳與二進位畫面 relay 流程
- 新增 Windows Agent 的桌面擷取與輸入注入能力
- 新增 MSSQL schema 與 `RemoteDesktopDevices`、`RemoteDesktopAgentPresenceLogs` 兩張核心表
- 修正根目錄建置流程，新增聚合 `RemoteDesktopSystem.csproj`，讓 `dotnet build` 可直接成功
