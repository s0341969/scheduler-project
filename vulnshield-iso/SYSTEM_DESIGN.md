# VulnShield-ISO 技術設計文件

## 1. 系統概述
VulnShield-ISO 是一套專為符合 ISO 27001 標準而設計的弱點管理系統。它將自動化掃描技術與嚴謹的風險管理流程結合，實現從「資產識別 $\rightarrow$ 漏洞掃描 $\rightarrow$ 風險評估 $\rightarrow$ 修復驗證 $\rightarrow$ 審計留痕」的閉環管理。

## 2. ISO 27001 合規映射表
本系統針對 ISO 27001 (特別是 A.12.6.1 技術漏洞管理) 實作了以下控制項：

| ISO 27001 控制要求 | 系統實作方案 | 程式碼實作位置 |
| :--- | :--- | :--- |
| **資產清冊 (Asset Inventory)** | 強制定義資產名稱、IP/網域、環境及所有者。 | \src/models/asset.py\ |
| **風險評估 (Risk Assessment)** | 採用 $\text{CVSS} \times \text{Criticality}$ 量化模型，非單一分數。 | \src/services/risk_calculator.py\ |
| **漏洞生命週期管理** | 實作 Open $\rightarrow$ Fixed $\rightarrow$ Verified 狀態機。 | \src/models/vulnerability.py\ |
| **職責分離 (Segregation of Duties)** | 僅限 Auditor 角色可執行漏洞關閉驗證。 | \src.api.main (update_finding_status)\ |
| **審計追蹤 (Audit Trail)** | 記錄所有狀態變更的不可篡改日誌。 | \src/models/scan.py (AuditLog)\ |
| **服務可用性 (Availability)** | 採用 Celery 異步隊列與併發控制，防止掃描導致系統崩潰。 | \src/worker/tasks.py\ |

## 3. 技術架構
### 3.1 技術棧
- **語言**: Python 3.11+ (FastAPI)
- **數據存儲**: PostgreSQL (強一致性)
- **任務隊列**: Celery + Redis (異步處理)
- **掃描引擎**: Nuclei (漏洞模板), Nmap (服務偵測)
- **部署**: Docker Compose

### 3.2 數據流向
1. **觸發**: 用戶通過 API 提交掃描請求 $\rightarrow$ 寫入 scan_tasks $\rightarrow$ 發送至 Celery 隊列。
2. **執行**: Worker 獲取任務 $\rightarrow$ 執行 Nmap $\rightarrow$ 根據端口啟動 Nuclei $\rightarrow$ 獲取 JSON 結果。
3. **持久化**: 解析結果 $\rightarrow$ 創建/更新 ulnerabilities $\rightarrow$ 計算風險分值 $\rightarrow$ 記錄 indings。
4. **閉環**: 安全分析師確認漏洞 $\rightarrow$ 開發人員修復 $\rightarrow$ 審計員驗證並關閉 $\rightarrow$ 寫入 udit_logs。

## 4. 部署指南
### 4.1 快速啟動
\\\ash
docker-compose up -d --build
\\\

### 4.2 API 使用流程
1. **建立資產**: POST /assets (設定 criticality 為 1-5)。
2. **啟動掃描**: POST /scans/trigger (傳入 sset_id)。
3. **追蹤進度**: GET /scans/{id}/status。
4. **查看風險**: GET /reports/iso27001。
5. **驗證修復**: PATCH /findings/{id}/status (需 Auditor 權限)。

## 5. 維護與擴展
- **新增掃描模板**: 僅需在 Worker 容器內更新 Nuclei 模板庫，無需修改代碼。
- **增加掃描工具**: 繼承 \BaseScanner\ 並在 \	asks.py\ 的管線中加入新服務。

