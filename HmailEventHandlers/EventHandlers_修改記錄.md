# EventHandlers.vbs 修改記錄

**文件日期：** 2026-05-12
**適用版本：** WhiteDomainPriority / UTF16 HTML Attachment / spam-report Learning

---

## 修改總覽

| # | 類型 | 說明 | 影響函式 |
|---|------|------|----------|
| Fix 1 | 程式錯誤 | 移除重複定義的 `IsWhiteDomainAnySender` 函式 | `IsWhiteDomainAnySender` |
| Fix 2 | 邏輯錯誤 | 合併內部放行邏輯，讓 `EnableTrustedInternalMailPass` 開關真正有效 | `OnAcceptMessage`、`IsInternalToInternalMail` |
| Fix 3 | 死碼清除 | 移除已無用的 `IsTrustedInternalMail` 函式 | `IsTrustedInternalMail` |
| Fix 4 | 功能補強 | 內部回覆白名單外部信件時，略過 MIS 通知 | `OnAcceptMessage`、新增 `AreAllRecipientsWhiteDomain` |

---

## Fix 1 — 移除重複定義的 `IsWhiteDomainAnySender`

### 問題描述

`IsWhiteDomainAnySender` 函式在原始檔案中出現兩次，內容完全相同。

VBScript 不支援函式重複定義，執行時會以**最後一份**定義為準，雖然行為結果相同，但屬於程式碼錯誤，且會造成語法檢查（Check Syntax）警告。

### 修正方式

保留第一份定義（較前面），刪除第二份重複定義。

### 修正前

```vbscript
' 第一份（行 ~1300）
Function IsWhiteDomainAnySender(ByVal FSO, ByVal oMessage)
    ...
End Function

' 第二份（行 ~1411）← 重複，刪除此份
Function IsWhiteDomainAnySender(ByVal FSO, ByVal oMessage)
    ...
End Function
```

### 修正後

```vbscript
' 只保留一份
Function IsWhiteDomainAnySender(ByVal FSO, ByVal oMessage)
    ...
End Function
```

---

## Fix 2 — 合併內部放行邏輯，讓開關真正有效

### 問題描述

原始流程中存在兩段邏輯完全相同的內部放行判斷：

1. `IsInternalToInternalMail`（**無開關**，無條件執行）
2. `IsTrustedInternalMail`（受 `EnableTrustedInternalMailPass` 開關控制）

由於第 1 段先執行且沒有開關，只要三個條件成立（IP 內部 + 寄件者內部 + 收件者全內部）就直接 `Exit Sub`，導致第 2 段**永遠到不了**。

這造成一個維護陷阱：管理員將 `EnableTrustedInternalMailPass` 改為 `False`，以為可以停用內部放行，但實際上完全無效。

### 三個條件

| 條件 | 說明 |
|------|------|
| 來源 IP 是內部 IP | 10.x.x.x / 192.168.x.x / 172.16–31.x.x / 127.0.0.1 |
| 寄件者是內部信箱 | 結尾符合 `@mail.gongin.com.tw` 或 `@gongin.com.tw` |
| 所有收件者都是內部信箱 | 任一外部收件者即不符合 |

### 修正方式

將 `IsInternalToInternalMail` 改為受 `EnableTrustedInternalMailPass` 開關控制，兩段邏輯合併為一，並移除多餘的 `IsTrustedInternalMail` 呼叫。

### 修正前（OnAcceptMessage 內）

```vbscript
' 第一段：無開關，永遠執行
If IsInternalToInternalMail(oClient, oMessage) Then
    Call WriteLog(...)
    Exit Sub
End If

' 第二段：有開關，但永遠到不了（死碼）
If EnableTrustedInternalMailPass = True Then
    If IsTrustedInternalMail(oClient, oMessage) Then
        Call WriteLog(...)
        Exit Sub
    End If
End If
```

### 修正後（OnAcceptMessage 內）

```vbscript
' 合併為一段，統一由開關控制
If EnableTrustedInternalMailPass = True Then
    If IsInternalToInternalMail(oClient, oMessage) Then
        Call WriteLog(...)
        Exit Sub
    End If
End If
```

---

## Fix 3 — 移除死碼 `IsTrustedInternalMail` 函式

### 問題描述

Fix 2 修正後，`IsTrustedInternalMail` 函式已無任何地方呼叫，成為死碼。

保留死碼會造成維護混淆，日後閱讀程式碼時容易誤以為此函式仍在使用。

### 修正方式

直接刪除 `IsTrustedInternalMail` 函式定義。

---

## Fix 4 — 內部回覆白名單外部信件時略過 MIS 通知

### 問題描述

**情境重現：**

1. 外部信件（例如 `yaohwa.com.tw`）含有高風險連結（`azurestaticapps.net`、`#@` 等）
2. 該外部網域已加入 `White_Domain.txt`，收到時正確放行，不標記 ✓
3. 內部員工**回覆**此信件（RE:）
4. 回覆信的寄件者是**內部信箱**，不在白名單 → 白名單檢查不命中
5. 系統判斷為**內部寄外部（Outbound）**，掃描信件內容
6. 回覆鏈帶入原始信的高風險內容 → 判定高風險 → 通知 MIS ❌

**核心原因：**

白名單只檢查**寄件者**，不檢查**收件者**。內部員工回覆時，寄件者是內部信箱，白名單自然不命中。

### 修正方式

新增函式 `AreAllRecipientsWhiteDomain`，在 Outbound 高風險路徑加入收件者白名單判斷：

- 所有收件者均在白名單 → 視為回覆給信任對象，**略過 MIS 通知**，只寫 Log
- 有任一收件者不在白名單 → 維持原本行為，通知 MIS

### 新增函式

```vbscript
Function AreAllRecipientsWhiteDomain(ByVal FSO, ByVal oMessage)
    ' 逐一檢查每個收件者，若有任一不在白名單則回傳 False
    Dim i, addr
    AreAllRecipientsWhiteDomain = False
    If oMessage.Recipients.Count = 0 Then Exit Function
    For i = 0 To oMessage.Recipients.Count - 1
        addr = oMessage.Recipients(i).Address
        If Not IsWhiteDomain(FSO, addr) Then
            AreAllRecipientsWhiteDomain = False
            Exit Function
        End If
    Next
    AreAllRecipientsWhiteDomain = True
End Function
```

### 修正後流程（Outbound 高風險路徑）

```
內部寄外部 + 判定高風險
    │
    ├─ 所有收件者都在 White_Domain.txt
    │      → 寫 Log（Outbound WhiteDomain Recipients SKIP）
    │      → 不通知 MIS ✓
    │
    └─ 有任一收件者不在 White_Domain.txt
           → 寫 Log（Outbound High Risk Notify Only）
           → 通知 MIS（維持原行為）
```

### Log 範例

**修正後，回覆白名單外部信件時 Log 會出現：**

```
2026/05/12 14:18:41 Outbound WhiteDomain Recipients SKIP: From=user@gongin.com.tw Subject=RE: [高風險詐騙] 耀管會11505董事聲明書(中文版)修改 Score=23 Reason=AutoBlockDomain=azurestaticapps.net; ...
```

**不再產生 MIS 通知信。**

---

## 行數統計

| 版本 | 總行數 |
|------|--------|
| 原始版本 | 3,376 行 |
| Fix 1–3 修正後 | 3,301 行（刪減 75 行重複/死碼） |
| Fix 4 修正後 | 3,365 行（新增 64 行功能） |
