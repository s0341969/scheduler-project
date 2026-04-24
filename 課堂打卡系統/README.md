# 課堂打卡系統

這是一個以 ASP.NET Core MVC (`.NET 9`) 建置的課堂打卡系統，目標是提供單機即可執行的教室出席管理工具，不依賴外部資料庫即可使用。

## 目前功能

- 課堂儀表板：顯示今日與近期課堂、開放狀態、已到人數與課程資訊。
- 學生打卡：輸入學號、姓名、備註即可完成打卡。
- 防止重複打卡：同一堂課同一學號只能成功打卡一次。
- 管理者登入：使用 Cookie 驗證保護後台功能。
- 課程與課堂後台：可新增、編輯、刪除課程與課堂時段。
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
- 若需多教室、多管理者或跨設備同步，建議下一階段改接資料庫與身分驗證。

## 資料檔案

- 執行期資料：`App_Data/attendance-data.json`
- 此檔案不納入 Git 版本控制，首次啟動會自動產生。

## 後續擴充方向

- 多管理者與角色權限
- 更換管理者密碼 UI
- 補簽退與請假流程
- 課堂打卡網址短網址化
- 圖表化出席分析
