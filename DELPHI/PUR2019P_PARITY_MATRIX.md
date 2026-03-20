# PUR2019P 功能對照矩陣

來源：`G:\GON\GON\PUR2019\PUR2019P.pas`

狀態說明：
- `Y`：已落地到 C#
- `P`：部分落地
- `N`：尚未落地

## 核心流程
| Delphi Procedure | 目的 | 狀態 | 備註 |
|---|---|---|---|
| `SpeedButton1Click` | 新增單頭 | Y | 已對應 C# CreateOrder |
| `SpeedButton2Click` | 進入查詢 | P | 已可查詢，尚未完全複製舊查詢欄位互動 |
| `SpeedButton4Click` | 存檔 | P | 單頭/單身存檔可用，尚未完全複製 dataset state 流程 |
| `SpeedButton5Click` | 確認 | Y | 含可選 SP 檢核 |
| `SpeedButton7Click` | 取消確認 | Y | 含 `PURDEL` 防護 |
| `SpeedButton8Click` | 執行查詢 | Y | 已對應 QueryHeaders |
| `SpeedButton9Click` / `SpeedButton10Click` | 上一筆/下一筆 | Y | 已加入 UI 導覽 |
| `SpeedButton11Click` | 印表 | P | 已可輸出報表文字，Delphi 報表 SQL 尚未完整移植 |
| `SpeedButton12Click` | 作廢 | Y | 已含單頭狀態與刪除單身 |

## 單頭資料事件
| Delphi Procedure | 目的 | 狀態 | 備註 |
|---|---|---|---|
| `Query1NewRecord` | 新單預設值 | P | 已有預設值，部分欄位尚未補齊 |
| `Query1BeforePost` | 單頭儲存前檢核 | P | 主要欄位已檢核，預算檢核未完整 |
| `Query1BeforeDelete` | 單頭刪除檢核 | Y | 狀態/單身存在檢核已做 |
| `Query1BeforeEdit` | 編輯權限/狀態限制 | P | 狀態限制已做，權限細節未完整 |
| `Query1AfterScroll` | 單頭切換後刷新 | P | 已刷新單身與摘要，預算顯示未完整 |

## 單身資料事件
| Delphi Procedure | 目的 | 狀態 | 備註 |
|---|---|---|---|
| `Query3NewRecord` | 單身預設值 | Y | 已含序號與預設欄位 |
| `Query3BeforeDelete` | 單身刪除檢核 | Y | 含狀態與發料關聯防護 |
| `Query3BeforePost` | 單身儲存前檢核 | P | 已有製令狀態、PUPA、MOQ；其餘欄位連動仍缺 |
| `Query3CalcFields` | 計算欄位 | P | 成本比/參考金額已做，其他計算欄位未完整 |
| `wwDBGrid1ColExit` | 欄位離開連動 | P | 部分邏輯移植，細節未全覆蓋 |

## 系統/介面事件
| Delphi Procedure | 目的 | 狀態 | 備註 |
|---|---|---|---|
| `FormCreate` | 啟動初始化 | P | 已有文化與資料來源初始化，權限載入未完整 |
| `FormKeyDown` | F2~F12 快捷鍵 | Y | C# 已加入快捷鍵 |
| `FormCloseQuery` | 關閉前檢查 | P | 已加入未存檔提示；暫存資料刪除流程未完整移植 |
| `SpeedButton6Click` | 關閉 | Y | 已對應關閉視窗 |
| `ALMailSlot1NewMessage` | 訊息顯示 | N | 尚未移植 |

## 外部模組相依
| 模組 | 狀態 | 備註 |
|---|---|---|
| `CHECKBUGDA` 預算邏輯 | P | 主流程框架已接，細節公式待補 |
| 報表輸出（`#ATEMP`, `Query12/13`） | P | 已可輸出文字報表，Delphi SQL 暫存表流程尚未移植 |
| `Utility.pas` 共用函式 | P | 部分已內化到服務層 |
| `PUR2019AP*` 管理端 | N | 尚未完整移植 |
