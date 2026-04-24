# Changelog

## 2026-04-24

- 移除對 `pytest tmp_path` fixture 與預設快取的依賴，改用專案內自建臨時目錄，避免 Windows 權限限制導致測試失敗。
- 補齊專案文件基線檔案 `CHANGELOG.md` 與 `TODO.md`。
