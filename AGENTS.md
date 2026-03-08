## 專案固定規範（每次都要遵守）

在開始任何修改前，先讀 `README.md`、`CHANGELOG.md`、`TODO.md`。

1. 每次修改功能後，必須同步更新以下文件：
- `README.md`（重要決策與目前行為）
- `CHANGELOG.md`（修改紀錄）
- `TODO.md`（待辦事項）

2. 每次修改完成後，必須先執行：
- `dotnet build`
若失敗，先修到成功再回報。

3. UI 顯示文字預設使用繁體中文（zh-TW）。

4. 回報格式必須包含：
- 修改的檔案清單
- 是否通過 `dotnet build`
- 文件是否已同步更新（README/CHANGELOG/TODO）

5. 每次修改完成後，需自動執行 Git 提交流程（目標 `origin main`）：
- `git add -A`
- `git commit -m "<YYYY-MM-DD HH:mm + 變更摘要>"`
- `git push origin main`
- 若無變更可提交，則略過 `commit/push` 並在回報中說明原因。
