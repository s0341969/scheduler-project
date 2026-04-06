# TODO

## High Priority

- 若臺灣銀行未來調整 HTML 結構，補上更精準的 DOM 解析策略或改接正式資料來源。
- 增加抓取失敗重試與退避策略，避免短暫網路異常造成長時間缺資料。
- 在 `DataGridView` 上加入查詢或排序輔助，方便快速定位指定幣別。

## Medium Priority

- 支援 Windows 開機自動啟動後最小化到系統匣。
- 增加現金匯率異常波動告警。
- 增加匯出 CSV 功能。

## Low Priority

- 加入代理伺服器設定。
- 顯示每種幣別最近一次成功寫入時間。
- 若 `CHRNAME` 或 `CHRNAME-HISTORY` 實際 schema 與目前假設不同，補上正式 DB 欄位驗證與型別調整。
- 取得正式資料庫可登入帳號後，實際比對 `TEST.dbo.CHRNAME` 與 `TEST.dbo.[CHRNAME-HISTORY]` 欄位定義。
