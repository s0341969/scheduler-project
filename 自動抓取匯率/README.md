# 臺灣銀行匯率自動抓取（WinForms + MSSQL）

## 目標

本專案提供一個可直接用 Visual Studio 2015 開啟的 Windows Forms 桌面程式，用來定時抓取臺灣銀行牌告匯率頁面，並寫入 MSSQL。

## 設計重點

1. 專案格式採傳統 .NET Framework WinForms 專案，可在 VS2015 直接開啟，後續可用 Designer 拖拉元件。
2. 抓取來源固定為 `https://rate.bot.com.tw/xrt?Lang=zh-TW`，預設抓取的是牌告匯率頁的最新資料。
3. 畫面可設定：
   - MSSQL 連線字串
   - 抓取頻率數值
   - 抓取頻率單位（分鐘 / 小時）
   - HTTP 逾時秒數
4. 連線字串若未在畫面填寫，會自動 fallback 讀取環境變數 `BOT_RATE_DB_CONN`。
5. Timer 觸發時若上一輪尚未完成，會自動略過，避免重疊執行。
6. MSSQL 寫入採唯一索引去重，鍵值為 `SourceUpdatedAt + CurrencyCode`，可避免同一掛牌時間重複寫入。

## 資料表

執行 [`sql/001_Create_BotExchangeRateTables.sql`](sql/001_Create_BotExchangeRateTables.sql) 或在畫面按「初始化資料庫」後，會建立：

- `dbo.BotExchangeRateSnapshot`

欄位摘要：

- `SourceRateDate`：頁面顯示日期
- `SourceUpdatedAt`：牌價最新掛牌時間
- `CurrencyCode`：幣別代碼，例如 `USD`
- `CurrencyName`：幣別名稱，例如 `美金`
- `CashBuy` / `CashSell`
- `SpotBuy` / `SpotSell`
- `SourceUrl`
- `InsertedAtUtc`

## 使用方式

1. 用 Visual Studio 2015 開啟 `BotExchangeRateWinForms.csproj`。
2. 編譯後啟動程式。
3. 在畫面輸入 MSSQL 連線字串，或先設定環境變數 `BOT_RATE_DB_CONN`。
4. 先按「初始化資料庫」建立資料表。
5. 設定抓取頻率，例如：
   - `30` + `分鐘`
   - `1` + `小時`
6. 按「立即抓取一次」可先測試。
7. 按「啟動 Timer」後，程式會依設定持續抓取。

## 建置

在本專案目錄執行：

```powershell
dotnet build
```

## 注意事項

1. 臺灣銀行頁面上的「下載 CSV」目前可能導向維護頁，因此本專案直接抓 HTML 並解析表格。
2. 解析邏輯依賴現行牌告匯率表格結構；若臺銀未來改版，可能需要調整 `Services/BotExchangeRateScraper.cs`。
3. 建議將連線字串指向專用資料庫或專用 schema，不要直接寫入正式交易系統核心表。
