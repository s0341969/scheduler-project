# 臺灣銀行匯率自動抓取（WinForms + MSSQL）

## 目標

本專案提供一個可直接用 Visual Studio 2015 開啟的 Windows Forms 桌面程式，用來定時抓取臺灣銀行牌告匯率頁面，並可選擇是否寫入 MSSQL。

## 設計重點

1. 專案格式採傳統 .NET Framework WinForms 專案，可在 VS2015 直接開啟，後續可用 Designer 拖拉元件。
2. 抓取來源固定為 `https://rate.bot.com.tw/xrt?Lang=zh-TW`。
3. 畫面顯示只保留「現金匯率」兩個欄位：
   - `本行買入`
   - `本行賣出`
4. 寫入資料庫時，會同時處理：
   - `dbo.CHRNAME`
   - `dbo.[CHRNAME-HISTORY]`
5. 寫入 `FTIL / FTOL` 的規則依你提供的舊邏輯：
   - `CNY`：使用現金買入 / 現金賣出
   - 其他幣別：使用即期買入 / 即期賣出
6. `CHRNAM` 會依既有規則轉換，例如：
   - `USD -> US`
   - `JPY -> JP`
   - `THB -> TA`
   - `CNY -> RMB`
   - `MYR -> MA`
   - `TWD -> NT$`
7. 畫面可設定：
   - MSSQL 連線字串
   - 是否寫入資料庫
   - 抓取頻率數值
   - 抓取頻率單位（分鐘 / 小時）
   - HTTP 逾時秒數
8. 「寫入 MSSQL 資料庫」預設為未勾選。未勾選時，程式只抓取資料並顯示結果，不會連線或寫入 MSSQL。
9. 每次抓取成功後，畫面下方的 `DataGridView` 會顯示最新匯率資料，包含幣別、現金本行買入、現金本行賣出、掛牌日期與更新時間。
10. 連線字串若未在畫面填寫，會自動 fallback 讀取環境變數 `BOT_RATE_DB_CONN`。
11. Timer 觸發時若上一輪尚未完成，會自動略過，避免重疊執行。

## 資料表

執行 [sql/001_Create_BotExchangeRateTables.sql](C:\codex_pg\自動抓取匯率\sql\001_Create_BotExchangeRateTables.sql) 或在畫面按「初始化資料庫」後，會建立：

- `dbo.CHRNAME`
- `dbo.[CHRNAME-HISTORY]`

欄位對應：

`CHRNAME`
- `CHRNAM`
- `CHRDS`
- `FTIL`
- `FTOL`
- `CRDATE`

`CHRNAME-HISTORY`
- `CHRNAM`
- `FTIL`
- `FTOL`
- `CRDATE`

## 使用方式

1. 用 Visual Studio 2015 開啟 [BotExchangeRateWinForms.csproj](C:\codex_pg\自動抓取匯率\BotExchangeRateWinForms.csproj)。
2. 編譯後啟動程式。
3. 若只想先測試抓取，保持「寫入 MSSQL 資料庫」未勾選即可。
4. 若要寫入資料庫，在畫面輸入 MSSQL 連線字串，或先設定環境變數 `BOT_RATE_DB_CONN`，並勾選「寫入 MSSQL 資料庫」。
5. 先按「初始化資料庫」建立 `CHRNAME` 與 `CHRNAME-HISTORY`。
6. 設定抓取頻率，例如：
   - `30` + `分鐘`
   - `1` + `小時`
7. 按「立即抓取一次」可先測試，成功後結果會直接顯示在畫面表格。
8. 按「啟動 Timer」後，程式會依設定持續抓取，表格會更新為最近一次成功抓取結果。

## 建置

在本專案目錄執行：

```powershell
dotnet build
```

## 注意事項

1. 臺灣銀行頁面上的「下載 CSV」目前可能導向維護頁，因此本專案直接抓 HTML 並解析表格。
2. 畫面顯示使用現金匯率，但 DB 寫入規則依既有程式邏輯，`CNY` 與其他幣別的來源欄位不同。
3. 解析邏輯依賴現行牌告匯率表格結構；若臺銀未來改版，可能需要調整 [BotExchangeRateScraper.cs](C:\codex_pg\自動抓取匯率\Services\BotExchangeRateScraper.cs)。
4. 若你的正式 DB schema 與目前假設不同，請以正式資料庫欄位定義為準再微調。
