# VulnScan.Web 操作手冊

## 1. 文件目的

本文件提供 `VulnScan.Web` 的日常操作方式，適用於：

- 系統管理員
- 弱點掃描人員
- 資安主管
- 稽核查核人員

本手冊以目前 `G:\codex_pg\vulnshield-iso\VulnScan.Web` 的真實功能為準。

---

## 2. 系統功能概覽

`VulnScan.Web` 是一套企業弱點掃描與弱點管理平台，主要功能包括：

- 資產清冊管理
- 掃描白名單控管
- 建立與執行掃描任務
- 解析 `Nmap` 掃描結果
- 匯入 `Nuclei`、`Nessus`、`Greenbone / OpenVAS` 弱點資料
- 弱點清單與改善追蹤
- Excel / PDF 報表匯出
- 稽核紀錄保存

---

## 3. 啟動系統

### 3.1 一鍵啟動

直接執行：

```bat
G:\codex_pg\vulnshield-iso\start_vulnscan_web.bat
```

啟動後系統預設網址為：

- 登入頁：`http://localhost:5186/Auth/Login`

### 3.2 開發環境資料庫

開發模式預設使用：

- `SQLite`
- DB 檔案位置：`G:\codex_pg\vulnshield-iso\VulnScan.Web\App_Data\vulnscan-dev.db`

### 3.3 預設登入帳號

Development 預設帳號：

- `admin / Admin123!Demo`
- `secmgr / Security123!Demo`
- `scanner / Scanner123!Demo`
- `viewer / Viewer123!Demo`

---

## 4. 首次環境確認

### 4.1 確認 Nmap 是否已安裝

若要執行內建掃描，Windows 主機必須先安裝 `Nmap`。

系統會依序尋找：

1. `VulnScan:NmapPath`
2. 系統 `PATH`
3. 常見安裝路徑：
   - `C:\Program Files (x86)\Nmap\nmap.exe`
   - `C:\Program Files\Nmap\nmap.exe`
   - `C:\Nmap\nmap.exe`

若未安裝，掃描時會出現：

```text
找不到 Nmap 執行檔。請先安裝 Nmap，或在 VulnScan 設定中把 NmapPath 指向有效的 nmap.exe。
```

現在系統已補上執行前檢查：

- 進入 `掃描任務` 頁時，若找不到 `Nmap`，畫面上方會先顯示阻擋警示
- `立即掃描` 按鈕會停用
- 其他透過系統建立 `ScanRun` 的流程，也會在建立前先被攔住，而不是等背景工作失敗後才知道

### 4.2 建議做法

建議先確認：

```powershell
where.exe nmap
```

若找不到，請先安裝 `Nmap`。

---

## 5. 主要選單說明

登入後左側主要選單包含：

- `儀表板`
- `資產清冊`
- `掃描白名單`
- `掃描任務`
- `掃描紀錄`
- `Port / Service`
- `弱點清單`
- `自動匯入`
- `Greenbone 整合`
- `改善追蹤`
- `報表匯出`
- `稽核紀錄`

---

## 6. 資產清冊操作

### 6.1 新增資產

進入：

- `資產清冊`

填寫主要欄位：

- `資產名稱`
- `IP 位址`
- `資產類型`
- `是否啟用`

建議命名方式：

- `SQL.76`
- `FW-HQ-01`
- `NAS-ACCT-01`

### 6.2 編輯資產

在資產清單中點選：

- `編輯`

可修改：

- 名稱
- IP
- 類型
- 啟用狀態

### 6.3 刪除資產

在資產清單中點選：

- `刪除`

刪除前請確認該資產是否已有歷史弱點資料與掃描紀錄。

---

## 7. 掃描白名單操作

### 7.1 功能目的

系統只允許掃描白名單範圍內的目標。

若目標不在白名單，系統會拒絕建立掃描。

### 7.2 新增白名單範圍

進入：

- `掃描白名單`

填寫：

- `RangeName`
- `CIDR`
- `Description`
- `IsEnabled`

範例：

- `10.1.0.0/16`
- `192.168.0.0/24`

---

## 8. 掃描任務操作

### 8.1 建立掃描任務

進入：

- `掃描任務`

填寫：

- `JobName`
- `TargetRange`
- `ScanType`
- `ScanTool`
- `ScanProfile`
- `IsEnabled`

目前 `ScanProfile` 會影響 `Nmap` 參數：

- `Low`：`-sV`
- `Deep`：`-sV -O --version-all`
- 其他：`-sV -O`

### 8.2 觸發掃描

在掃描任務列表中執行：

- `Run`

系統會：

1. 建立 `ScanRun`
2. 丟入 `Hangfire`
3. 背景執行 `Nmap`
4. 解析 XML
5. 寫入 `AssetPorts`

### 8.3 掃描失敗常見原因

- 目標不在白名單
- `Nmap` 未安裝
- `nmap.exe` 路徑錯誤
- 目標設備無法連線

---

## 9. 掃描紀錄與 Port / Service

### 9.1 掃描紀錄

進入：

- `掃描紀錄`

可查看：

- `RunId`
- `狀態`
- `開始時間`
- `結束時間`
- `錯誤訊息`
- `RawResultPath`

### 9.2 Port / Service

進入：

- `Port / Service`

可查看：

- 主機
- Port
- Protocol
- Service
- Product
- Version

---

## 10. 弱點清單

### 10.1 弱點來源

弱點目前可來自：

- `Nuclei JSON / JSONL`
- `Nessus CSV / XML`
- `Greenbone / OpenVAS GMP API`

### 10.2 欄位說明

弱點清單目前顯示：

- `資產`
- `IP`
- `弱點名稱`
- `軟體版本`
- `特徵碼版本`
- `Severity`
- `CVSS`
- `狀態`
- `負責人`

### 10.3 版本欄位定義

- `軟體版本`
  - 代表系統從掃描輸出中解析出的產品版本
- `特徵碼版本`
  - 代表掃描器的特徵碼、Feed 或 NVT 版本

---

## 11. 弱點改善追蹤

### 11.1 進入方式

從：

- `弱點清單`

點選：

- `詳情 / 改善`

### 11.2 可維護欄位

可更新：

- `狀態`
- `負責人`
- `改善期限`
- `備註`
- `附件`

### 11.3 改善紀錄

也可於：

- `改善追蹤`

查看所有弱點改善動作歷史。

---

## 12. 自動匯入

### 12.1 自動匯入頁面

進入：

- `自動匯入`

可查看：

- Nuclei incoming 路徑
- Nessus incoming 路徑
- Greenbone API 來源摘要
- 最近自動匯入紀錄
- 最近已處理檔案
- 最近失敗檔案

### 12.2 目錄匯入路徑

預設路徑：

- `G:\codex_pg\vulnshield-iso\VulnScan.Web\App_Data\AutoImport\Nuclei\incoming`
- `G:\codex_pg\vulnshield-iso\VulnScan.Web\App_Data\AutoImport\Nessus\incoming`

### 12.3 立即執行一次

可手動按：

- `立即執行一次`

系統會立即處理：

- 目錄中的 Nuclei / Nessus 檔案
- 已啟用的 Greenbone API 同步

---

## 13. Greenbone 整合

### 13.1 進入方式

進入：

- `Greenbone 整合`

### 13.2 可設定項目

可設定：

- `啟用同步`
- `主機位址`
- `Port`
- `登入帳號`
- `密碼`
- `忽略憑證錯誤`
- `每次同步報表數`
- `報表篩選條件`
- `結果篩選條件`

### 13.3 操作流程

1. 先填好 Greenbone 連線資訊
2. 按 `儲存設定`
3. 按 `立即同步一次`
4. 到下方 `最近同步明細` 查看結果

### 13.4 同步明細欄位

同步紀錄會保存：

- `時間`
- `模式`
- `狀態`
- `端點`
- `報表`
- `任務`
- `匯入筆數`
- `訊息`

### 13.5 失敗時怎麼看

若同步失敗，請先看：

- `訊息`

常見原因包括：

- 主機位址錯誤
- 帳密錯誤
- TLS 憑證問題
- 報表格式異常
- 報表內容無法解析

---

## 14. 報表匯出

### 14.1 Excel 匯出

進入：

- `報表匯出`

可匯出：

- 高風險弱點 Excel

內容包含：

- `IP 位址`
- `弱點名稱`
- `Severity`
- `軟體版本`
- `特徵碼版本`
- `改善期限`

### 14.2 PDF 匯出

在 `報表匯出` 頁可選擇：

- `開始日期`
- `結束日期`

按：

- `匯出 PDF 報表`

PDF 內容目前包含：

- 報表期間
- 弱點總數
- 高風險弱點
- 受影響資產
- 弱點明細表
- `軟體版本`
- `特徵碼版本`

### 14.3 匯出檔案位置

報表預設輸出到：

- `VulnScan:ResultRootPath\reports`

開發預設通常是：

- `C:\VulnScan\Results\reports`

---

## 15. 稽核紀錄

進入：

- `稽核紀錄`

可查看：

- 誰做了什麼操作
- 什麼時間做
- 影響了哪個物件

常見操作包括：

- 建立掃描任務
- 建立掃描執行紀錄
- Greenbone 設定變更
- 報表匯出
- 弱點狀態更新

---

## 16. 常見問題

### 16.1 Nmap 啟動錯誤

錯誤範例：

```text
An error occurred trying to start process 'nmap'
```

代表：

- 系統找不到 `nmap.exe`

處理方式：

1. 安裝 `Nmap`
2. 確認 `where.exe nmap` 能找到路徑
3. 若仍找不到，在設定中指定 `VulnScan:NmapPath`

### 16.2 掃描白名單錯誤

錯誤範例：

```text
Target `10.1.1.76` 不在白名單內
```

代表：

- 目標 IP 不在允許掃描範圍

處理方式：

1. 到 `掃描白名單`
2. 新增對應的 CIDR 範圍

### 16.3 Greenbone 同步失敗

優先檢查：

1. `Host / Port`
2. 帳號密碼
3. TLS / 憑證設定
4. `ReportFilter / ResultFilter`
5. 同步明細中的 `訊息`

---

## 17. 建議作業流程

日常作業建議順序：

1. 維護 `資產清冊`
2. 確認 `掃描白名單`
3. 確認 `Nmap` 已安裝
4. 建立或執行 `掃描任務`
5. 匯入 `Nuclei / Nessus / Greenbone`
6. 到 `弱點清單` 查看結果
7. 到 `改善追蹤` 指派負責人與期限
8. 到 `報表匯出` 輸出 Excel / PDF
9. 到 `稽核紀錄` 查核操作歷程

---

## 18. 文件維護說明

本手冊目前對應：

- `G:\codex_pg\vulnshield-iso\VulnScan.Web`
- 截至 `2026-06-05` 的功能狀態

若後續功能新增：

- `UsersController`
- `EF Core Migration`
- `Greenbone 測試連線`
- 更完整 PDF 版型

需同步更新本手冊。
