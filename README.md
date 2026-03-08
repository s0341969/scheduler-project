# Production Scheduler (MSSQL + 工序相依)

這個版本可直接從 MSSQL 三張表讀資料，用 Python 計算排程，輸出 CSV 結果。

## 已支援

- 機台狀態過濾（只排可用機台）
- 定品訂機優先分派
- 同製卡工序相依（sequence 先後不可逆排）
- 有限產能前推排程（機台最早可用）

## 安裝

```powershell
pip install -r C:\codex_pg\requirements.txt
```

## 執行（MSSQL）

```powershell
set PROD_SCHEDULER_CONN=Driver={ODBC Driver 17 for SQL Server};Server=YOUR_SERVER;Database=YOUR_DB;UID=YOUR_USER;PWD=YOUR_PASSWORD
python C:\codex_pg\run_db_schedule.py --output C:\codex_pg\schedule_output.csv
```

可選參數：

```powershell
python C:\codex_pg\run_db_schedule.py --machine-table 機台資料表 --fixed-table 定品訂機表 --work-table 工作表 --as-of "2026-03-06 08:00:00"
```

## 輸出欄位

- `job_id`
- `sequence`
- `order_id`
- `machine_id`
- `start_at`
- `end_at`
- `tardiness_hours`

## 欄位對應（來源表）

- 機台資料表：`機台編號` `機台製程代號` `機台狀態`
- 定品訂機表：`機台名稱` `產品圖號` `產品製程`
- 工作表：`製卡` `圖號` `製程順序` `製程代號` `製程名稱` `預估工時`

## 備註

- 工序相依以 `製卡 + 製程順序` 建立。
- 目前 `定品訂機表.機台名稱` 需能對應到 `機台資料表.機台編號`。
