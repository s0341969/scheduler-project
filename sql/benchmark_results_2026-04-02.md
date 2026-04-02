# Benchmark Results (2026-04-02)

資料庫：`10.1.1.76 / TEST`  
程序：`dbo.產生ORDE3剩餘製程`  
測試方式：`sqlcmd` 單次執行（每個情境 1 次），以 PowerShell Stopwatch 量測 wall-clock。

| 情境 | INPART | WallClockMs | 約分鐘 |
|---|---|---:|---:|
| 單一製卡 | `24X01008MT-0%` | 356,571 | 5.94 |
| 特定前綴批次 | `23G%` | 350,177 | 5.84 |
| 全量 | `%` | 345,210 | 5.75 |

執行時間（Asia/Taipei）：
- 2026-04-02 00:42:37
- 2026-04-02 00:48:28
- 2026-04-02 00:54:13

## 第四輪優化後 `%` 連跑 2 次（批次化 `-1000/-500` 時間差）

| Run | INPART | WallClockMs | 約分鐘 |
|---|---|---:|---:|
| 1 | `%` | 330,745 | 5.51 |
| 2 | `%` | 352,789 | 5.88 |
| 平均 | `%` | 341,767 | 5.70 |

對照第三輪基準（`344,045 ms`）平均改善：`2,278 ms`（`0.66%`，無顯著差異）。

## 第五輪優化後 `%` 連跑 2 次（HM/PM set-based + temp index）

| Run | INPART | WallClockMs | 約分鐘 |
|---|---|---:|---:|
| 1 | `%` | 340,553 | 5.68 |
| 2 | `%` | 338,533 | 5.64 |
| 平均 | `%` | 339,543 | 5.66 |

對照第三輪基準（`344,045 ms`）平均改善：`4,502 ms`（`1.31%`，小幅改善）。

## 第六輪實驗 `%` 連跑 3 次（HM 單一序號表整併，已回退）

| Run | INPART | WallClockMs | 約分鐘 |
|---|---|---:|---:|
| 1 | `%` | 341,229 | 5.69 |
| 2 | `%` | 377,418 | 6.29 |
| 3 | `%` | 355,876 | 5.93 |
| 平均 | `%` | 358,174 | 5.97 |

結論：未優於第五輪穩定版（平均 `339,543 ms`），已回退到第五輪版本。

## 第七輪定位 `%` 單次（HM 子段細分里程碑）

| 指標 | ms |
|---|---:|
| TOTAL_MS | 359,394 |
| AfterOutsourcePhase -> BeforeHMSection | 25,803 |
| BeforeHMSection -> AfterHMWorkBuild | 157 |
| AfterHMWorkBuild -> AfterHMClassify | 47 |
| AfterHMClassify -> AfterHMAssignCore | 46 |
| AfterHMAssignCore -> AfterHMSchedule | 0 |

結論：HM 子段不是瓶頸，熱點在 HM 前段（CMM/LQ 等流程）。
## 第八輪定位 `%` 單次（CMM/LQ 細分里程碑）

| 指標 | ms |
|---|---:|
| TOTAL_MS | 321,848 |
| AfterOutsourcePhase -> AfterDlytimeOPhase | 6,268 |
| AfterDlytimeOPhase -> AfterCMMSchedule | 19,227 |
| AfterCMMSchedule -> AfterLQSchedule | 125 |
| AfterLQSchedule -> BeforeHMSection | 0 |

測試時間（Asia/Taipei）：2026-04-03 00:09~00:15  
對應 log：`_perf_run_2026-04-03_000928_stage8_cmm_lq_split.log`

結論：本區段瓶頸已定位在 CMM 排程，LQ/HM 前銜接不是主要耗時來源。

