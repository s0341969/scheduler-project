# VulnScan Web 弱點掃描管理平台開發規格書

版本：V1.0  
用途：提供 Codex / 開發人員依規格建立 Web 版弱點掃描管理平台  
建議專案名稱：`VulnScan.Web`  
主要技術：ASP.NET Core MVC / Web API、MSSQL、Hangfire、Nmap、OpenVAS / Greenbone、Nuclei、Nessus 匯入  

---

## 0. 開發前重要聲明

本系統僅用於**企業內部已授權資產**之弱點掃描、資產盤點、改善追蹤與稽核報表產出。

系統不得設計成攻擊工具，不得提供以下功能：

- 未授權外部 IP 掃描
- 暴力破解
- 密碼噴灑
- DoS / DDoS 測試
- Exploit 利用程式執行
- 自動入侵
- 橫向移動
- 憑證竊取
- Web Shell 上傳
- 繞過權限控管

所有掃描目標必須先通過白名單檢查，所有操作必須寫入稽核紀錄。

---

## 1. 系統目標

建立一套 Web 版弱點掃描管理平台，用於企業內部：

1. 資產清冊管理
2. 掃描範圍白名單控管
3. 掃描任務排程
4. Port / Service 掃描
5. 弱點掃描結果匯入
6. CVE / CVSS / EPSS / VPR 風險資料管理
7. 弱點改善追蹤
8. 複掃驗證
9. Dashboard 儀表板
10. Excel / PDF 報表匯出
11. ISO 27001 稽核佐證

---

## 2. 參考資料設計重點

本系統規格參考以下觀念：

### 2.1 白箱掃描與黑箱掃描區分

白箱掃描主要針對原始碼與靜態程式碼進行檢查；黑箱掃描則由外部角度檢查系統服務、Web 系統與主機弱點。

本系統主要負責：

- 黑箱弱點掃描管理
- 資產盤點
- 弱點改善追蹤
- 報表管理

白箱掃描可列為未來擴充，例如 SonarQube、Puma Scan、SAST 工具整合。

### 2.2 弱點掃描與滲透測試區分

弱點掃描偏向自動化工具檢測已知弱點。  
滲透測試偏向人工驗證、攻擊路徑分析與漏洞利用驗證。

本系統定位為弱點掃描與改善追蹤平台，不做滲透測試攻擊功能。

### 2.3 弱掃報告設計重點

弱掃報告需包含：

- 掃描日期
- 掃描範圍
- 掃描工具
- 資產列表
- 弱點總覽
- 風險等級統計
- 弱點詳細說明
- 修補建議
- 改善狀態
- 複掃結果
- 稽核佐證

### 2.4 商業弱掃工具功能參考

參考 Nessus 類型工具功能，本系統需保留以下欄位擴充能力：

- CVE
- CVSS
- CVSS v4
- EPSS
- VPR
- Plugin ID
- Plugin Name
- Risk Factor
- Solution
- Evidence
- Compliance Check
- Report Export

---

## 3. 系統定位

系統名稱：`VulnScan Web`  
系統類型：企業內部弱點掃描管理平台  
部署型態：內部部署 On-Premise  
使用者：資訊部、資安人員、系統負責人、主管、稽核人員  

系統流程：

```text
資產盤點
↓
掃描白名單確認
↓
建立掃描任務
↓
背景執行掃描
↓
匯入掃描結果
↓
弱點分級
↓
指派改善
↓
改善追蹤
↓
複掃驗證
↓
報表輸出
```

---

## 4. 建議技術架構

### 4.1 第一版建議

```text
ASP.NET Core MVC
MSSQL
Hangfire
Nmap
Bootstrap
ClosedXML
DinkToPdf 或 QuestPDF
```

### 4.2 正式版建議

```text
Frontend：ASP.NET Core MVC / Vue 3
Backend：ASP.NET Core Web API
Database：MSSQL
Background Job：Hangfire
Scanner Node：Linux Docker / Windows Agent
Scan Tools：Nmap / OpenVAS / Greenbone / Nuclei / Nessus Import
Report：Excel / PDF
Auth：Local Account / AD / LDAP
```

---

## 5. 系統架構圖

```text
使用者瀏覽器
    ↓
ASP.NET Core MVC / Web API
    ↓
MSSQL Database
    ↓
Hangfire Background Jobs
    ↓
Scanner Agent
    ↓
Nmap / OpenVAS / Greenbone / Nuclei / Nessus Import
    ↓
XML / JSON / CSV Result Parser
    ↓
Assets / Ports / Vulnerabilities / Reports
```

---

## 6. 系統模組

```text
VulnScan.Web
├─ Dashboard 儀表板
├─ Assets 資產清冊
├─ ScanAllowedRanges 掃描白名單
├─ ScanJobs 掃描任務
├─ ScanRuns 掃描執行紀錄
├─ AssetPorts 開放 Port 結果
├─ Vulnerabilities 弱點清單
├─ VulnerabilityActions 改善追蹤
├─ RescanTasks 複掃驗證
├─ Reports 報表匯出
├─ Users 帳號權限
├─ AuditLogs 操作紀錄
└─ SystemSettings 系統設定
```

---

## 7. 使用角色與權限

### 7.1 角色定義

| 角色 | 說明 |
|---|---|
| Admin | 系統管理員，可管理全部功能 |
| Security | 資安人員，可建立掃描、查看弱點、執行複掃 |
| ITOwner | 系統負責人，只能查看與處理自己負責資產 |
| Manager | 主管，只能查看 Dashboard 與報表 |
| Auditor | 稽核人員，只能查看報表與紀錄 |

### 7.2 權限矩陣

| 功能 | Admin | Security | ITOwner | Manager | Auditor |
|---|---:|---:|---:|---:|---:|
| Dashboard | Y | Y | Y | Y | Y |
| 資產清冊 | Y | Y | Own | View | View |
| 掃描白名單 | Y | View | N | N | View |
| 掃描任務 | Y | Y | Request | View | View |
| 立即掃描 | Y | Y | N | N | N |
| 掃描紀錄 | Y | Y | Own | View | View |
| 弱點清單 | Y | Y | Own | View | View |
| 改善追蹤 | Y | Y | Own | View | View |
| 複掃驗證 | Y | Y | Request | View | View |
| 報表匯出 | Y | Y | Own | Y | Y |
| 帳號管理 | Y | N | N | N | N |
| 系統設定 | Y | N | N | N | N |

---

## 8. 功能需求

## 8.1 Dashboard 儀表板

### 功能說明

首頁需讓主管與資安人員快速掌握整體弱點狀態。

### 顯示卡片

- 資產總數
- 已掃描資產數
- 高風險資產數
- Critical 弱點數
- High 弱點數
- 未處理弱點數
- 逾期未改善弱點數
- 本月已改善弱點數
- 最近一次掃描時間

### 圖表

- 弱點風險等級圓餅圖
- 每月弱點趨勢圖
- 單位弱點排行
- 資產風險排行 Top 10
- 逾期改善排行

### 最近資料表

- 最近掃描任務
- 最新發現高風險弱點
- 即將到期改善項目
- 複掃未通過項目

---

## 8.2 資產清冊管理

### 功能

- 新增資產
- 修改資產
- 停用資產
- 查詢資產
- 匯入 Excel
- 匯出 Excel
- 查看資產弱點歷史
- 查看資產 Port 歷史
- 查看最後掃描時間

### 欄位

| 欄位 | 型態 | 必填 | 說明 |
|---|---|---:|---|
| AssetID | int | Y | PK |
| AssetName | nvarchar(100) | Y | 資產名稱 |
| HostName | nvarchar(100) | N | 主機名稱 |
| IPAddress | varchar(50) | Y | IP 位址 |
| AssetType | nvarchar(50) | Y | 資產類型 |
| OSInfo | nvarchar(200) | N | 作業系統 |
| DeviceBrand | nvarchar(100) | N | 品牌 |
| DeviceModel | nvarchar(100) | N | 型號 |
| OwnerDept | nvarchar(100) | N | 負責單位 |
| OwnerUser | nvarchar(100) | N | 負責人 |
| Importance | nvarchar(20) | N | High / Medium / Low |
| NetworkZone | nvarchar(50) | N | 網段區域 |
| IsActive | bit | Y | 是否啟用 |
| LastScanTime | datetime | N | 最後掃描時間 |

### 資產類型

```text
Windows Server
Linux Server
MSSQL Server
Web Server
Firewall
Switch
Router
NAS
VPN Gateway
Printer
IoT Device
Other
```

---

## 8.3 掃描白名單管理

### 功能

- 新增允許掃描網段
- 修改允許掃描網段
- 停用允許掃描網段
- 掃描前檢查目標是否在白名單內

### 規則

1. 所有掃描目標必須在 `ScanAllowedRanges` 內。
2. 不允許任意掃描外部 IP。
3. 不允許任意掃描外部網域。
4. 若目標不在白名單，系統需阻擋並寫入 AuditLogs。
5. 白名單只允許 Admin 維護。
6. CIDR 檢查需支援單一 IP 與網段。

### 範例

| RangeName | CIDR | Description |
|---|---|---|
| MIS Server Zone | 10.1.1.0/24 | 內部伺服器網段 |
| Office Network | 10.1.2.0/24 | 辦公室網段 |
| Factory Network | 10.2.2.0/24 | 廠區網段 |
| DMZ Zone | 10.1.10.0/24 | DMZ 網段 |

---

## 8.4 掃描任務管理

### 功能

- 建立掃描任務
- 修改掃描任務
- 啟用 / 停用掃描任務
- 手動立即執行
- 排程執行
- 查看任務歷史
- 複製任務

### 掃描類型

| 掃描類型 | 說明 |
|---|---|
| DiscoveryScan | 資產探索 |
| PortScan | 開放 Port 掃描 |
| ServiceVersionScan | 服務版本偵測 |
| VulnerabilityScan | 弱點掃描 |
| ComplianceCheck | 合規檢查 |
| WebScan | Web 弱點檢查 |
| SSLScan | SSL / TLS 檢查 |
| Rescan | 複掃 |

### 掃描工具

```text
Nmap
OpenVAS
Greenbone
Nuclei
NessusImport
Manual
```

### 掃描強度

| Profile | 說明 |
|---|---|
| Low | 低風險，資產探索與 Banner 檢查 |
| Normal | 一般弱點掃描 |
| Deep | 深層掃描，需核准 |
| Compliance | 合規設定檢查 |
| Web | Web 服務檢查 |
| Rescan | 針對已改善弱點複掃 |

### 排程類型

```text
Manual
Daily
Weekly
Monthly
Once
```

---

## 8.5 掃描執行紀錄

### 功能

- 每次執行掃描建立 ScanRun
- 記錄狀態
- 記錄開始與結束時間
- 記錄錯誤訊息
- 記錄原始報告路徑
- 顯示掃描摘要

### 狀態

```text
Pending
Running
Completed
Failed
Cancelled
```

### 掃描執行限制

1. Web Controller 不得直接同步執行掃描。
2. 掃描需交給 Hangfire 背景任務。
3. 執行前需檢查白名單。
4. 掃描失敗需更新 ScanRuns.Status = Failed。
5. 掃描完成需更新統計數量。

---

## 8.6 Port / Service 掃描結果

### 功能

- 顯示每台資產開放 Port
- 顯示服務名稱與版本
- 顯示掃描時間
- 可依 Port 查詢
- 可依資產查詢
- 可標示高風險 Port

### 高風險 Port 提示

| Port | 服務 | 提示 |
|---:|---|---|
| 21 | FTP | 明文傳輸，確認必要性 |
| 22 | SSH | 建議限制來源 |
| 23 | Telnet | 高風險，建議停用 |
| 80 | HTTP | 建議導向 HTTPS |
| 443 | HTTPS | 檢查 TLS / 憑證 |
| 445 | SMB | 注意橫向移動與 SMB 弱點 |
| 1433 | MSSQL | 建議限制來源 |
| 3389 | RDP | 建議 VPN / MFA / 限制來源 |

---

## 8.7 弱點清單管理

### 功能

- 匯入 OpenVAS / Greenbone 結果
- 匯入 Nessus CSV / XML
- 匯入 Nuclei JSON
- 手動新增弱點
- 編輯弱點狀態
- 指派負責人
- 設定改善期限
- 查看弱點詳細資訊
- 查看弱點歷史
- 標記誤判
- 標記風險接受

### 弱點欄位

| 欄位 | 說明 |
|---|---|
| VulnID | 弱點流水號 |
| AssetID | 資產 ID |
| RunID | 掃描執行 ID |
| IPAddress | IP |
| PortNumber | Port |
| ServiceName | 服務 |
| CVE | CVE 編號 |
| PluginID | 工具 Plugin ID |
| VulnName | 弱點名稱 |
| Severity | Critical / High / Medium / Low / Info |
| CVSS | CVSS 分數 |
| CVSSVersion | CVSS 版本 |
| EPSS | 漏洞被利用機率 |
| VPR | 弱點優先評級 |
| Description | 弱點說明 |
| Solution | 修補建議 |
| Evidence | 掃描證據 |
| Status | 狀態 |
| OwnerUser | 負責人 |
| DueDate | 改善期限 |
| FirstDetectedAt | 首次發現 |
| LastDetectedAt | 最後發現 |

### 弱點狀態

```text
未處理
已指派
改善中
待複掃
複掃通過
複掃未通過
風險接受
誤判
已關閉
```

---

## 8.8 風險分級與改善期限

### 預設分級

| 等級 | 條件 | 改善期限 |
|---|---|---|
| Critical | CVSS >= 9.0 | 7 天 |
| High | CVSS >= 7.0 and < 9.0 | 14 天 |
| Medium | CVSS >= 4.0 and < 7.0 | 30 天 |
| Low | CVSS < 4.0 | 90 天 |
| Info | 無 CVSS 或資訊類 | 視情況 |

### 優先處理邏輯

優先排序分數建議依下列條件加權：

1. Severity
2. CVSS
3. EPSS
4. VPR
5. 資產重要性
6. 是否外部可連線
7. 是否有公開利用程式
8. 是否影響核心系統
9. 是否逾期
10. 是否重複出現

---

## 8.9 改善追蹤

### 功能

- 新增改善紀錄
- 修改改善紀錄
- 上傳附件
- 更新弱點狀態
- 指派處理人
- 記錄處理日期
- 申請複掃

### 常見改善內容範例

```text
已安裝 Windows Update
已更新 Linux 套件
已升級 OpenSSL
已停用 TLS 1.0 / 1.1
已關閉 Telnet
已限制 RDP 來源
已更新防火牆韌體
已調整 Web Server Header
已修正弱密碼設定
已停用不必要服務
已關閉不必要 Port
```

---

## 8.10 複掃驗證

### 流程

```text
負責人改善完成
↓
狀態改為待複掃
↓
資安人員執行複掃
↓
系統比對弱點是否仍存在
↓
若不存在：複掃通過，狀態改為已關閉
↓
若仍存在：複掃未通過，退回改善中
```

### 複掃結果狀態

```text
Pass
Fail
NotFound
ManualReview
FalsePositive
```

---

## 8.11 報表匯出

### 報表清單

| 報表名稱 | 格式 | 用途 |
|---|---|---|
| 弱點掃描總表 | Excel / PDF | 內部管理 |
| 高風險弱點清單 | Excel | 主管追蹤 |
| 逾期未改善清單 | Excel | 責任追蹤 |
| 資產風險排行 | Excel / PDF | 管理審查 |
| 單一資產弱點報告 | PDF | 系統負責人改善 |
| 複掃驗證報告 | PDF | 改善佐證 |
| ISO 27001 稽核報表 | PDF | 稽核佐證 |
| 月度弱點趨勢報表 | PDF | 管理審查 |

### 報表內容

- 報表產出日期
- 掃描區間
- 掃描工具
- 掃描範圍
- 掃描資產數
- 弱點總數
- 風險等級統計
- 高風險弱點明細
- 各單位弱點分布
- 改善狀態統計
- 逾期未改善清單
- 複掃結果
- 修補建議
- 產出人員

---

## 9. 資料庫設計

> 資料庫：MSSQL  
> 建議資料庫名稱：`VulnScanDB`

---

### 9.1 Users

```sql
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Account NVARCHAR(50) NOT NULL UNIQUE,
    UserName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200),
    RoleName NVARCHAR(50) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

### 9.2 Assets

```sql
CREATE TABLE Assets (
    AssetID INT IDENTITY(1,1) PRIMARY KEY,
    AssetName NVARCHAR(100) NOT NULL,
    HostName NVARCHAR(100),
    IPAddress VARCHAR(50) NOT NULL,
    AssetType NVARCHAR(50),
    OSInfo NVARCHAR(200),
    DeviceBrand NVARCHAR(100),
    DeviceModel NVARCHAR(100),
    OwnerDept NVARCHAR(100),
    OwnerUser NVARCHAR(100),
    Importance NVARCHAR(20),
    NetworkZone NVARCHAR(50),
    IsExternalFacing BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    LastScanTime DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

### 9.3 ScanAllowedRanges

```sql
CREATE TABLE ScanAllowedRanges (
    RangeID INT IDENTITY(1,1) PRIMARY KEY,
    RangeName NVARCHAR(100) NOT NULL,
    CIDR VARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    IsEnabled BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

### 9.4 ScanJobs

```sql
CREATE TABLE ScanJobs (
    JobID INT IDENTITY(1,1) PRIMARY KEY,
    JobName NVARCHAR(100) NOT NULL,
    TargetRange NVARCHAR(500) NOT NULL,
    ScanType NVARCHAR(50) NOT NULL,
    ScanTool NVARCHAR(50) NOT NULL,
    ScanProfile NVARCHAR(50),
    ScheduleType NVARCHAR(50),
    ScheduleTime TIME NULL,
    CronExpression NVARCHAR(100) NULL,
    IsEnabled BIT DEFAULT 1,
    CreatedBy NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);
```

---

### 9.5 ScanRuns

```sql
CREATE TABLE ScanRuns (
    RunID INT IDENTITY(1,1) PRIMARY KEY,
    JobID INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NULL,
    Status NVARCHAR(50) NOT NULL,
    TotalHosts INT DEFAULT 0,
    TotalOpenPorts INT DEFAULT 0,
    TotalVulnerabilities INT DEFAULT 0,
    ErrorMessage NVARCHAR(MAX),
    RawResultPath NVARCHAR(500),
    CreatedBy NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

---

### 9.6 AssetPorts

```sql
CREATE TABLE AssetPorts (
    PortID INT IDENTITY(1,1) PRIMARY KEY,
    AssetID INT NOT NULL,
    RunID INT NOT NULL,
    IPAddress VARCHAR(50),
    PortNumber INT,
    Protocol VARCHAR(10),
    ServiceName NVARCHAR(100),
    ServiceProduct NVARCHAR(200),
    ServiceVersion NVARCHAR(200),
    State NVARCHAR(50),
    RiskHint NVARCHAR(500),
    DetectedAt DATETIME DEFAULT GETDATE()
);
```

---

### 9.7 Vulnerabilities

```sql
CREATE TABLE Vulnerabilities (
    VulnID INT IDENTITY(1,1) PRIMARY KEY,
    AssetID INT NOT NULL,
    RunID INT NOT NULL,
    IPAddress VARCHAR(50),
    CVE NVARCHAR(50),
    PluginID NVARCHAR(100),
    VulnName NVARCHAR(300),
    Severity NVARCHAR(20),
    CVSS DECIMAL(3,1) NULL,
    CVSSVersion NVARCHAR(20) NULL,
    EPSS DECIMAL(8,6) NULL,
    VPR DECIMAL(3,1) NULL,
    PortNumber INT NULL,
    ServiceName NVARCHAR(100),
    Description NVARCHAR(MAX),
    Solution NVARCHAR(MAX),
    Evidence NVARCHAR(MAX),
    Status NVARCHAR(50) DEFAULT N'未處理',
    OwnerUser NVARCHAR(100),
    DueDate DATE NULL,
    FirstDetectedAt DATETIME DEFAULT GETDATE(),
    LastDetectedAt DATETIME DEFAULT GETDATE(),
    ClosedAt DATETIME NULL
);
```

---

### 9.8 VulnerabilityActions

```sql
CREATE TABLE VulnerabilityActions (
    ActionID INT IDENTITY(1,1) PRIMARY KEY,
    VulnID INT NOT NULL,
    ActionUser NVARCHAR(100),
    ActionStatus NVARCHAR(50),
    ActionNote NVARCHAR(MAX),
    AttachmentPath NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

---

### 9.9 AuditLogs

```sql
CREATE TABLE AuditLogs (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    UserAccount NVARCHAR(50),
    ActionType NVARCHAR(100),
    TargetType NVARCHAR(100),
    TargetID INT NULL,
    SourceIPAddress VARCHAR(50),
    LogMessage NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

---

### 9.10 ReportExports

```sql
CREATE TABLE ReportExports (
    ExportID INT IDENTITY(1,1) PRIMARY KEY,
    ReportName NVARCHAR(200),
    ReportType NVARCHAR(50),
    FilePath NVARCHAR(500),
    ExportedBy NVARCHAR(50),
    ExportedAt DATETIME DEFAULT GETDATE()
);
```

---

## 10. 後端專案架構

```text
VulnScan.Web
├─ Controllers
│  ├─ DashboardController.cs
│  ├─ AssetsController.cs
│  ├─ ScanAllowedRangesController.cs
│  ├─ ScanJobsController.cs
│  ├─ ScanRunsController.cs
│  ├─ AssetPortsController.cs
│  ├─ VulnerabilitiesController.cs
│  ├─ VulnerabilityActionsController.cs
│  ├─ ReportsController.cs
│  ├─ UsersController.cs
│  └─ AuditLogsController.cs
│
├─ Models
│  ├─ User.cs
│  ├─ Asset.cs
│  ├─ ScanAllowedRange.cs
│  ├─ ScanJob.cs
│  ├─ ScanRun.cs
│  ├─ AssetPort.cs
│  ├─ Vulnerability.cs
│  ├─ VulnerabilityAction.cs
│  ├─ AuditLog.cs
│  └─ ReportExport.cs
│
├─ Data
│  └─ ApplicationDbContext.cs
│
├─ Services
│  ├─ NmapService.cs
│  ├─ ScanJobService.cs
│  ├─ ScanAllowedRangeService.cs
│  ├─ NmapXmlParserService.cs
│  ├─ NessusImportService.cs
│  ├─ NucleiImportService.cs
│  ├─ VulnerabilityService.cs
│  ├─ RiskScoringService.cs
│  ├─ ReportService.cs
│  └─ AuditLogService.cs
│
├─ ViewModels
│  ├─ DashboardViewModel.cs
│  ├─ AssetViewModel.cs
│  ├─ ScanJobViewModel.cs
│  ├─ ScanRunViewModel.cs
│  └─ VulnerabilityViewModel.cs
│
├─ Views
│  ├─ Dashboard
│  ├─ Assets
│  ├─ ScanAllowedRanges
│  ├─ ScanJobs
│  ├─ ScanRuns
│  ├─ AssetPorts
│  ├─ Vulnerabilities
│  ├─ Reports
│  └─ Shared
│
├─ wwwroot
│  ├─ css
│  ├─ js
│  └─ lib
│
└─ appsettings.json
```

---

## 11. 服務類別規格

### 11.1 NmapService

負責呼叫 Nmap 並產生 XML。

```csharp
public interface INmapService
{
    Task<string> RunNmapAsync(string target, string outputPath, string scanProfile);
}
```

必要規則：

- 不可直接接受未檢查的 target。
- target 需先通過白名單。
- outputPath 需寫入系統設定指定目錄。
- 執行失敗需拋出明確錯誤。

---

### 11.2 ScanAllowedRangeService

負責檢查 IP / CIDR 是否允許掃描。

```csharp
public interface IScanAllowedRangeService
{
    Task<bool> IsTargetAllowedAsync(string target);
}
```

必要規則：

- 支援單一 IP。
- 支援 CIDR。
- 支援 IP 範圍。
- 不支援外部網域掃描，除非白名單允許。
- 不符合規則時，需寫入 AuditLogs。

---

### 11.3 ScanJobService

負責建立 ScanRun 並呼叫背景任務。

```csharp
public interface IScanJobService
{
    Task<int> CreateRunAsync(int jobId, string userAccount);
    Task RunScanAsync(int runId);
}
```

必要規則：

- `CreateRunAsync` 建立狀態為 Pending。
- `RunScanAsync` 開始時改為 Running。
- 成功時改為 Completed。
- 失敗時改為 Failed 並寫入 ErrorMessage。

---

### 11.4 NmapXmlParserService

負責解析 Nmap XML。

```csharp
public interface INmapXmlParserService
{
    Task ParseAndSaveAsync(int runId, string xmlPath);
}
```

需解析：

- IP
- Hostname
- Port
- Protocol
- Service Name
- Product
- Version
- State
- OS Guess

---

### 11.5 VulnerabilityService

負責弱點資料統一化。

```csharp
public interface IVulnerabilityService
{
    Task ImportVulnerabilityAsync(VulnerabilityImportDto dto);
    Task AssignOwnerAsync(int vulnId, string ownerUser);
    Task UpdateStatusAsync(int vulnId, string status, string note, string userAccount);
}
```

---

### 11.6 ReportService

負責產生 Excel / PDF。

```csharp
public interface IReportService
{
    Task<string> ExportVulnerabilityExcelAsync(DateTime start, DateTime end);
    Task<string> ExportHighRiskExcelAsync();
    Task<string> ExportIso27001PdfAsync(DateTime start, DateTime end);
}
```

---

## 12. 掃描流程規格

### 12.1 手動立即掃描流程

```text
使用者按下立即掃描
↓
系統讀取 ScanJob
↓
檢查 TargetRange 是否在白名單
↓
不合法：拒絕並寫入 AuditLogs
↓
合法：建立 ScanRun，狀態 Pending
↓
Hangfire Enqueue
↓
背景任務更新狀態 Running
↓
呼叫 Nmap / Scanner
↓
產生 XML / JSON / CSV
↓
Parser 解析
↓
寫入 AssetPorts / Vulnerabilities
↓
更新 ScanRun 統計
↓
狀態改 Completed
```

---

### 12.2 掃描失敗流程

```text
背景任務發生錯誤
↓
捕捉 Exception
↓
ScanRuns.Status = Failed
↓
ScanRuns.ErrorMessage = Exception Message
↓
寫入 AuditLogs
↓
前端顯示錯誤訊息
```

---

### 12.3 改善追蹤流程

```text
弱點建立
↓
系統依 Severity 自動設定 DueDate
↓
Security 指派 Owner
↓
Owner 填寫改善紀錄
↓
狀態改為 待複掃
↓
Security 執行複掃
↓
Pass：狀態改 已關閉
↓
Fail：狀態改 複掃未通過
```

---

## 13. Nmap 執行策略

### 13.1 掃描參數

Low：

```bash
nmap -sV -oX result.xml TARGET
```

Normal：

```bash
nmap -sV -O -oX result.xml TARGET
```

Deep：

```bash
nmap -sV -O --version-all -oX result.xml TARGET
```

### 13.2 禁止參數

第一版禁止：

```text
--script vuln
--script exploit
--script brute
--script dos
```

除非後續正式核准並建立安全控管，否則不得開啟。

---

## 14. 匯入格式

### 14.1 Nmap XML

匯入至：

- Assets
- AssetPorts
- ScanRuns 統計

### 14.2 Nessus CSV / XML

匯入至：

- Vulnerabilities

需對應欄位：

| Nessus 欄位 | 系統欄位 |
|---|---|
| Plugin ID | PluginID |
| CVE | CVE |
| Risk | Severity |
| CVSS | CVSS |
| Host | IPAddress |
| Port | PortNumber |
| Protocol | Protocol |
| Name | VulnName |
| Description | Description |
| Solution | Solution |
| Plugin Output | Evidence |

### 14.3 Nuclei JSON

匯入至：

- Vulnerabilities

需對應欄位：

| Nuclei 欄位 | 系統欄位 |
|---|---|
| template-id | PluginID |
| info.name | VulnName |
| info.severity | Severity |
| matched-at | Evidence |
| host | IPAddress |
| extracted-results | Evidence |
| info.description | Description |
| info.remediation | Solution |

---

## 15. 前端頁面規格

### 15.1 Layout

左側選單：

```text
Dashboard
資產清冊
掃描白名單
掃描任務
掃描紀錄
開放 Port
弱點清單
改善追蹤
報表匯出
帳號管理
操作紀錄
系統設定
```

### 15.2 Dashboard

卡片：

- 資產總數
- 高風險資產
- Critical
- High
- 未處理
- 逾期
- 本月改善
- 最近掃描

### 15.3 資產清冊

功能按鈕：

- 新增
- 匯入 Excel
- 匯出 Excel
- 查看
- 編輯
- 停用

查詢條件：

- IP
- 主機名稱
- 資產類型
- 負責單位
- 負責人
- 風險等級

### 15.4 掃描任務

功能按鈕：

- 新增任務
- 編輯
- 啟用 / 停用
- 立即掃描
- 查看紀錄

### 15.5 弱點清單

查詢條件：

- Severity
- CVE
- IP
- Port
- 負責人
- 狀態
- 是否逾期
- 日期區間

功能：

- 查看詳細
- 指派負責人
- 更新狀態
- 新增改善紀錄
- 申請複掃
- 匯出

---

## 16. 安全需求

### 16.1 系統安全

1. 必須登入後才可使用。
2. 權限依角色限制。
3. 弱點資料不得公開。
4. 原始報告檔案需限制下載權限。
5. 報表下載需寫入 AuditLogs。
6. 掃描任務執行需寫入 AuditLogs。
7. 變更白名單需寫入 AuditLogs。
8. 密碼不得明文儲存。
9. Scanner 執行帳號採最小權限。
10. Web Server 與 Scanner Node 建議分離。

### 16.2 掃描安全

1. 只允許白名單目標。
2. 預設不執行破壞性測試。
3. 預設不做暴力破解。
4. 預設不做 DoS 類測試。
5. 核心設備建議維護時段掃描。
6. 掃描頻率需可控管。
7. 掃描結果需標示掃描時間。
8. 掃描失敗不得重複無限重試。

---

## 17. 開發階段規劃

### V1：資產盤點與 Port 掃描

必做：

- ASP.NET Core MVC 專案
- MSSQL 連線
- Entity Framework Core
- 資產清冊 CRUD
- 掃描白名單 CRUD
- 掃描任務 CRUD
- Hangfire 背景任務
- Nmap 呼叫
- Nmap XML 解析
- AssetPorts 顯示
- ScanRuns 顯示
- Excel 匯出
- AuditLogs

不做：

- OpenVAS API
- Nuclei 自動掃描
- Nessus API
- AD 登入
- PDF 報表
- Email 通知

---

### V2：弱點結果匯入

必做：

- Nessus CSV / XML 匯入
- OpenVAS / Greenbone XML 匯入
- Nuclei JSON 匯入
- 弱點清單統一格式
- CVE / CVSS / Severity 顯示
- 弱點詳細頁
- 風險分級
- 自動 DueDate

---

### V3：改善追蹤與複掃

必做：

- 指派負責人
- 改善紀錄
- 附件上傳
- 狀態流程
- 申請複掃
- 複掃結果比對
- 關閉弱點

---

### V4：Dashboard 與報表

必做：

- Dashboard 統計
- 弱點趨勢圖
- 資產風險排行
- 單位弱點統計
- 高風險報表
- 逾期未改善報表
- ISO 27001 PDF 報表

---

### V5：進階功能

可做：

- AD / LDAP 登入
- Email 通知
- 到期提醒
- 多 Scanner Node
- Scanner Node 健康狀態
- SIEM / SOC 拋轉
- API 串接修補系統

---

## 18. V1 驗收條件

| 項目 | 驗收標準 |
|---|---|
| 登入 | 未登入不得進入系統 |
| 資產清冊 | 可新增、修改、查詢、停用 |
| 掃描白名單 | 非白名單 IP 不可掃描 |
| 掃描任務 | 可建立手動掃描任務 |
| Hangfire | 掃描不造成 Web Timeout |
| Nmap | 可產生 XML |
| XML Parser | 可解析 IP、Port、Service、Version |
| Port 結果 | 可顯示資產開放 Port |
| 掃描紀錄 | 可查成功、失敗、錯誤訊息 |
| Excel 報表 | 可匯出 Port 掃描結果 |
| AuditLogs | 可記錄掃描、匯出、白名單異動 |
| 安全限制 | 不可掃描白名單外目標 |

---

## 19. Codex 開發指示

### 19.1 請優先完成 V1

請 Codex 依照以下順序開發：

1. 建立 ASP.NET Core MVC 專案 `VulnScan.Web`
2. 建立 MSSQL EF Core Models
3. 建立 DbContext
4. 建立 Migration 或 SQL Script
5. 建立 Bootstrap Layout
6. 建立資產清冊 CRUD
7. 建立掃描白名單 CRUD
8. 建立掃描任務 CRUD
9. 整合 Hangfire
10. 建立 NmapService
11. 建立 ScanAllowedRangeService
12. 建立 ScanJobService
13. 建立 NmapXmlParserService
14. 建立 ScanRuns 頁面
15. 建立 AssetPorts 頁面
16. 建立 AuditLogs
17. 建立 Excel 匯出

### 19.2 開發限制

- 不要實作攻擊利用功能。
- 不要實作暴力破解。
- 不要實作 DoS 掃描。
- 不要允許掃描白名單以外 IP。
- 不要在 Controller 內同步長時間執行掃描。
- 掃描必須走 Hangfire。
- 所有重要操作都要寫 AuditLogs。
- 所有資料存取都需透過 Service 或 DbContext。
- 所有輸入要驗證。
- 所有錯誤要記錄。

### 19.3 第一版不需要做的功能

- 不需要 AD 登入。
- 不需要 OpenVAS API。
- 不需要 Nessus API。
- 不需要 Nuclei 自動掃描。
- 不需要 PDF 報表。
- 不需要 Email 通知。
- 不需要多掃描節點。
- 不需要雲端部署。

---

## 20. appsettings.json 建議

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VulnScanDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "VulnScan": {
    "ResultRootPath": "C:\\VulnScan\\Results",
    "NmapPath": "nmap",
    "MaxConcurrentScans": 2,
    "AllowExternalTargets": false
  },
  "Hangfire": {
    "DashboardPath": "/hangfire"
  }
}
```

---

## 21. Program.cs 必要套件

建議 NuGet：

```text
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Hangfire
Hangfire.SqlServer
ClosedXML
System.Xml.Linq
Microsoft.AspNetCore.Authentication.Cookies
```

---

## 22. 首頁 Dashboard 範例畫面

```text
┌───────────────────────────────────────────────┐
│ VulnScan Web 弱點掃描管理平台                  │
├──────────┬──────────┬──────────┬──────────────┤
│ 資產總數 │ 高風險資產 │ 未處理弱點 │ 逾期未改善     │
│   128    │    12    │    45    │      6       │
├──────────┴──────────┴──────────┴──────────────┤
│ 弱點等級統計                                   │
│ Critical 3 / High 9 / Medium 21 / Low 42       │
├───────────────────────────────────────────────┤
│ 最近掃描任務                                   │
│ 內網掃描 10.1.1.0/24 Completed 2026-06-04      │
├───────────────────────────────────────────────┤
│ 高風險弱點 Top 10                              │
│ 10.1.1.10 SMB Signing Disabled High 未處理      │
└───────────────────────────────────────────────┘
```

---

## 23. V1 最小可交付成果

完成後系統至少需具備：

1. 使用者可登入
2. 可建立資產
3. 可建立掃描白名單
4. 可建立掃描任務
5. 可執行 Nmap 掃描
6. 非白名單目標會被拒絕
7. 掃描在 Hangfire 背景執行
8. 掃描結果 XML 可解析
9. Port 結果可查詢
10. 掃描紀錄可查詢
11. 可匯出 Excel
12. 可查操作紀錄

---

## 24. 參考來源

- 臺灣大學：資安掃描常見種類、白箱掃描與黑箱掃描說明  
  https://webpageprod.ntu.edu.tw/News_Content.aspx?n=24857&s=251133

- Tenable Nessus：弱點掃描器、CVSS、EPSS、VPR、報表與合規檢查功能參考  
  https://zh-tw.tenable.com/products/nessus

- 臺北區網中心弱點管理教育訓練簡報  
  https://www.tp1rc.edu.tw/tpnet2020/training/10904.pdf

- 桃園區網中心：如何看懂弱掃報告  
  https://www.tyrc.edu.tw/data/teach/lecture/%E5%A6%82%E4%BD%95%E7%9C%8B%E6%87%82%E5%BC%B1%E6%8E%83%E5%A0%B1%E5%91%8A.pdf

---

## 25. 後續可擴充項目

- OpenVAS / Greenbone API 串接
- Nessus API 串接
- Nuclei 自動掃描
- OWASP ZAP Web 掃描
- AD / LDAP 登入
- Email 到期提醒
- Teams / LINE Notify 通知
- PDF 稽核報表
- CVE 情資自動更新
- KEV 已知遭利用弱點標示
- 多掃描節點
- Scanner Agent 健康檢查
- SIEM / SOC 整合
- ISO 27001 報表範本
- 弱點改善 KPI
