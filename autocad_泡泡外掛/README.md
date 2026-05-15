# AutoBubble for AutoCAD

這個專案是一個以 `C# + AutoCAD .NET API` 實作的 AutoCAD 外掛起手架，用來自動將圖面中的編號文字加上泡泡圓圈。

目前提供的命令：

- `AUTOBUBBLE_SCAN`
  掃描目前空間內可辨識的數字文字，輸出候選清單到 AutoCAD 命令列。
- `AUTOBUBBLE_APPLY`
  自動掃描目前空間內所有可辨識的數字文字，為尚未處理的文字加上泡泡圓圈。
- `AUTOBUBBLE_PICK`
  只對使用者選取的 `DBText` / `MText` / `AttributeReference` 加上泡泡圓圈；若選到含屬性的圖塊，也會處理其屬性文字。

## 設計原則

- 預設只做 additive changes，不刪原文字、不重排既有圖元。
- 以 DWG 原生文字實體為主，不走 OCR。
- 對每個已處理的來源文字寫入 XData，避免重複標記。
- 寫入 XData 時保留來源物件原本屬於其他應用程式的 XData，不覆蓋既有資料。
- 透過獨立圖層 `AUTOBUBBLE` 管理所有自動生成的泡泡。

## 目錄結構

- `src/AutoBubble.Core`
  與 AutoCAD API 無關的設定、規則與資料模型。
- `src/AutoBubble.AutoCAD`
  AutoCAD 指令、DWG 實體掃描、泡泡建立、XData 標記。

## 環境需求

- Windows
- AutoCAD 2021 以上，建議 2023 / 2024 / 2025
- `.NET Framework 4.8 Developer Pack`
- Visual Studio 2022 或相容的 MSBuild 環境

## AutoCAD Managed DLL 設定

專案會從下列順序尋找 AutoCAD Managed DLL：

1. MSBuild 屬性 `AutoCadManagedDllDir`
2. 環境變數 `ACAD_MANAGED_DLL_DIR`
3. 預設安裝路徑：
   - `C:\Program Files\Autodesk\AutoCAD 2025`
   - `C:\Program Files\Autodesk\AutoCAD 2024`
   - `C:\Program Files\Autodesk\AutoCAD 2023`
   - `C:\Program Files\Autodesk\AutoCAD 2022`
   - `C:\Program Files\Autodesk\AutoCAD 2021`

如果你的 AutoCAD 安裝在其他位置，請在 PowerShell 先設定：

```powershell
$env:ACAD_MANAGED_DLL_DIR = "C:\Program Files\Autodesk\AutoCAD 2025"
```

## 建置

```powershell
dotnet build .\AutoBubble.sln -c Release
```

或使用：

```powershell
dotnet msbuild .\AutoBubble.sln /t:Build /p:Configuration=Release
```

## 載入到 AutoCAD

1. 開啟 AutoCAD
2. 執行 `NETLOAD`
3. 載入：

```text
src\AutoBubble.AutoCAD\bin\Release\AutoBubble.AutoCAD.dll
```

4. 執行命令：
   - `AUTOBUBBLE_SCAN`
   - `AUTOBUBBLE_APPLY`
   - `AUTOBUBBLE_PICK`

## 可調整行為

`src/AutoBubble.AutoCAD/autobubble.settings.json` 可調整：

- 自動泡泡圖層名稱
- 泡泡顏色
- 半徑 padding
- 支援的數字範圍
- 是否只接受純數字
- 最小 / 最大半徑

## 目前實作邏輯

1. 掃描目前空間中的 `DBText`、`MText`，以及 `BlockReference` 內的 `AttributeReference`
2. 將文字正規化後解析數字
3. 依文字幾何中心與範圍估算泡泡中心與半徑
4. 在 `AUTOBUBBLE` 圖層建立圓形
5. 於來源文字寫入 XData，標記來源已被處理與對應泡泡 Handle
6. 指令執行期間若發生例外，會在 AutoCAD 命令列顯示錯誤訊息，避免無訊息失敗

## 已知限制

- 目前第一版是「包住既有文字」的泡泡圓圈，不會替換為獨立 block 樣式。
- 如果文字的幾何範圍來自非標準字型或代理物件，AutoCAD extents 可能與視覺略有差異。

## 後續擴充方向

- 支援 `BlockReference` 的 `AttributeReference`
- 支援編號排序與缺號檢查
- 支援衝突避讓與自動偏移
- 支援預覽 / 套用 / 還原工作流
- 支援專案級圖層規則與命名規則
