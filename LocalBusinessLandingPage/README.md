# LocalBusinessLandingPage

提供「適合 Google 地圖商家網站連結使用」的單頁品牌著陸頁，採手機優先設計，主打可信度、地點便利、服務清楚與立即聯絡。

## 專案內容

- `wwwroot/index.html`：完整單頁著陸頁（HTML/CSS/JavaScript）。
- `Program.cs`：啟用靜態檔案服務，預設載入首頁。
- `/health`：簡易健康檢查端點。

## 已包含

- 繁體中文（zh-TW）文案。
- Hero、品牌簡介、核心服務、優勢、地點交通、服務承諾/FAQ、最終 CTA、Footer。
- 主要 CTA：`立即致電`、`路線導航`、`加入 LINE／立即預約`。
- SEO 基本結構：`title`、`meta description`、Open Graph、`LocalBusiness` JSON-LD。
- 手機優先響應式版型與柔和進場動畫（淡入、上滑、stagger reveal）。

## 預留替換欄位

- 公司名稱：[公司名稱]
- 主標題：[公司核心價值主張]
- 副標題：[一句話介紹服務與地點優勢]
- 服務項目：[服務一] [服務二] [服務三]
- 電話：[電話]
- 營業時間：[營業時間]
- 公司類型：[公司類型]
- LINE 或預約連結：[LINE或預約連結]
- 社群連結：[社群連結]

## 啟動

```powershell
cd C:\codex_pg\LocalBusinessLandingPage
dotnet run
```

開啟瀏覽器：`http://localhost:5000` 或終端顯示的 URL。
