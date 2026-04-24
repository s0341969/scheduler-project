# 課堂打卡系統

這是一個以 ASP.NET Core MVC (`.NET 9`) 建置的課堂打卡系統，目標是提供單機即可執行的教室出席管理工具，不依賴外部資料庫即可使用。

## 目前功能

- 課堂儀表板：顯示今日與近期課堂、開放狀態、已到人數與課程資訊。
- 課堂總覽分級分班：首頁固定依 `一年級 101-110`、`二年級 201-210`、`三年級 301-310` 顯示各班課堂。
- 學生本人登入：學生需先以學號密碼登入後才能打卡。
- 學生打卡：登入後僅可用自身帳號身分完成打卡，不可手動改學號姓名。
- 動態 QR Code：每堂課 QR Code 具時效限制，逾時必須重新掃描最新 QR。
- 防止重複打卡：同一堂課同一學號只能成功打卡一次。
- 班級 SSID 驗證：系統以來源 IP 是否落在班級 SSID 對應網段來判定。
- 裝置與來源記錄：保存 IP、User-Agent、裝置指紋與 QR 發放時間。
- 可疑紀錄標示：若不在班級 SSID 網段或同裝置近期被不同學號使用，後台會標示可疑。
- 管理者登入：使用 Cookie 驗證保護後台功能。
- 課程與課堂後台：可新增、編輯、刪除課程與課堂時段。
- 課程班級維護：管理者建立課程時可指定班級，支援 `101-110`、`201-210`、`301-310`。
- 課堂開關：管理者可直接開放或關閉某堂課的打卡。
- Excel 匯出：可下載單堂課的 `.xlsx` 出席紀錄。
- QR Code 打卡：可開啟課堂 QR Code 打卡板，供教室投影或列印使用。
- 本機持久化：資料儲存在 `App_Data/attendance-data.json`，首次啟動會自動建立種子資料。
- 繁體中文介面：預設使用 `zh-TW` 顯示日期與 UI 文字。

## 技術選型

- Framework: ASP.NET Core MVC, .NET 9
- 儲存方式: JSON 檔案持久化
- 驗證方式: DataAnnotations + Server-side validation
- 管理驗證: ASP.NET Core Cookie Authentication
- 學生驗證: 學生帳號登入 + 動態 QR + 網段比對
- Excel: ClosedXML
- QR Code: QRCoder
- 並發保護: `SemaphoreSlim` 保護檔案讀寫與打卡防重複邏輯

## 執行方式

```powershell
dotnet build
dotnet run
```

預設啟動後可透過主畫面查看課堂，並進入各課堂打卡頁面。

## 文件

- 操作手冊：`操作手冊.md`
- 截圖版操作手冊：`操作手冊_截圖版.md`

## 學生登入

- 學生登入入口：`/Student/Login`
- 目前示範帳號：
  - `S1123001 / Student123!`
  - `S1123002 / Student123!`
  - `S1123003 / Student123!`
- 2026-04-24 已修正登入表單 `ReturnUrl` 隱藏欄位被誤判必填，示範帳號可正常登入。

學生登入後，打卡頁會直接帶入登入學號與姓名，不允許自行修改。

## 管理者登入

- 後台入口：`/Admin`
- 預設帳號：`admin`
- 預設密碼：`ChangeMe123!`

此預設密碼僅適合初始測試或內部環境，正式使用前應立即改寫 `appsettings.json`、`appsettings.Development.json`，或用環境變數覆蓋：

```powershell
$env:AdminAuth__Username="admin"
$env:AdminAuth__Password="請改成正式密碼"
dotnet run
```

若要改用雜湊密碼，可填入：

- `AdminAuth__PasswordHashBase64`
- `AdminAuth__SaltBase64`
- `AdminAuth__Iterations`

## 系統假設

- 目前版本以單校區、單機部署為前提。
- 管理後台採單一管理者帳號模型。
- 打卡唯一鍵為 `課堂時段 + 學號`。
- 課程需隸屬於單一班級，首頁依班級代碼自動分配到對應年級區塊。
- 班級 SSID 驗證係透過對應 DHCP 網段判定，而非直接讀取 SSID 名稱。
- 若需多教室、多管理者或跨設備同步，建議下一階段改接資料庫與身分驗證。

## 資料檔案

- 執行期資料：`App_Data/attendance-data.json`
- 此檔案不納入 Git 版本控制，首次啟動會自動產生。

## 班級 SSID 驗證設定

目前班級 SSID 驗證使用：

- `AttendanceSecurity:AllowedIpPrefixes`
- `AttendanceSecurity:ExpectedSsidLabel`
- `AttendanceSecurity:StrictNetworkValidation`

範例：

```json
"AttendanceSecurity": {
  "QrTokenLifetimeMinutes": 5,
  "StrictNetworkValidation": false,
  "ExpectedSsidLabel": "學校班級 SSID",
  "AllowedIpPrefixes": [ "192.168.50.", "10.50." ]
}
```

說明：

- `AllowedIpPrefixes` 要改成你們班級 SSID 實際發出的 IP 網段
- `StrictNetworkValidation=false` 時，不在指定網段仍可打卡，但會被標示為可疑
- `StrictNetworkValidation=true` 時，不在指定網段將直接拒絕打卡

## 後續擴充方向

- 多管理者與角色權限
- 更換管理者密碼 UI
- 補簽退與請假流程
- 校務單一登入整合
- 圖表化出席分析
