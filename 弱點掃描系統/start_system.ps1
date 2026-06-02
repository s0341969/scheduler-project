Write-Host "🚀 正在啟動 VulnShield-ISO 系統..." -ForegroundColor Cyan

# 1. 啟動 Docker 容器
Write-Host "📦 步驟 1/4: 啟動 Docker 容器 (這可能需要幾分鐘下載鏡像)..." -ForegroundColor Yellow
docker compose up -d --build

# 2. 等待資料庫啟動
Write-Host "⏳ 步驟 2/4: 等待資料庫初始化..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# 3. 驗證 API 連通性
Write-Host "🛠️ 步驟 3/4: 驗證 API 連通性..." -ForegroundColor Yellow
try {
    $test_res = Invoke-RestMethod -Uri "http://localhost:8000/healthz" -Method Get -ErrorAction Stop
    Write-Host "✅ API 已就緒！" -ForegroundColor Green
} catch {
    Write-Host "❌ API 尚未完全啟動，請稍候 30 秒後再次嘗試，或檢查 docker logs。" -ForegroundColor Red
}

# 4. 打開瀏覽器
Write-Host "🌐 步驟 4/4: 開啟 API 互動文檔..." -ForegroundColor Yellow
Start-Process "http://localhost:8000/docs"

Write-Host "`n---------------------------------------------------" -ForegroundColor White
Write-Host "🎉 系統已啟動！" -ForegroundColor Green
Write-Host "👉 請在瀏覽器中查看 [http://localhost:8000/docs]" -ForegroundColor White
Write-Host "👉 預設管理員帳號請使用環境變數 DEFAULT_ADMIN_USERNAME / DEFAULT_ADMIN_PASSWORD" -ForegroundColor White
Write-Host "👉 建議操作流程: 先呼叫 /token 取得 Bearer Token，再建立資產與觸發掃描" -ForegroundColor White
Write-Host "---------------------------------------------------`n" -ForegroundColor White
