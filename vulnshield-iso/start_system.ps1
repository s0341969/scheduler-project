$ErrorActionPreference = 'Stop'

function Write-Step {
    param(
        [string]$Message
    )

    Write-Host $Message -ForegroundColor Yellow
}

function Fail-AndExit {
    param(
        [string]$Message
    )

    Write-Host $Message -ForegroundColor Red
    exit 1
}

function Invoke-CheckedCommand {
    param(
        [scriptblock]$Command,
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        Fail-AndExit $FailureMessage
    }
}

function Test-ComposeServiceRunning {
    param(
        [string]$ServiceName
    )

    $containerId = docker compose -p vulnshield-iso ps -q $ServiceName
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
        return $false
    }

    $running = docker inspect -f "{{.State.Running}}" $containerId 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $false
    }

    return $running.Trim().ToLowerInvariant() -eq 'true'
}

function Show-StartupDiagnostics {
    Write-Host ""
    Write-Host "Recent API logs:" -ForegroundColor Yellow
    docker compose -p vulnshield-iso logs --tail 80 api

    Write-Host ""
    Write-Host "Recent worker logs:" -ForegroundColor Yellow
    docker compose -p vulnshield-iso logs --tail 80 worker

    Write-Host ""
    Write-Host "Recent beat logs:" -ForegroundColor Yellow
    docker compose -p vulnshield-iso logs --tail 80 beat
}

Write-Host "Starting VulnShield-ISO..." -ForegroundColor Cyan

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$envFile = Join-Path $projectRoot '.env'
if (-not (Test-Path -LiteralPath $envFile)) {
    Fail-AndExit "Missing .env. Copy .env.example to .env first."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Fail-AndExit "Docker command not found. Install Docker Desktop first."
}

Write-Step "Step 1/5: Checking Docker..."
try {
    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Fail-AndExit "Docker Desktop is not running, or this account cannot access Docker."
    }
}
catch {
    Fail-AndExit "Docker Desktop is not running, or this account cannot access Docker."
}

Write-Step "Step 2/5: Starting containers..."
Invoke-CheckedCommand -Command { docker compose -p vulnshield-iso up -d --build } -FailureMessage "docker compose startup failed."

Write-Step "Step 3/5: Waiting for API, worker, and beat readiness..."
$healthUrl = 'http://localhost:8000/healthz'
$isHealthy = $false
$workerReady = $false
$beatReady = $false
$maxAttempts = 36

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    Start-Sleep -Seconds 5

    try {
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -ErrorAction Stop
        if ($response.status -eq 'ok') {
            $isHealthy = $true
        }
    }
    catch {
    }

    $workerReady = Test-ComposeServiceRunning -ServiceName 'worker'
    $beatReady = Test-ComposeServiceRunning -ServiceName 'beat'
    if ($isHealthy -and $workerReady -and $beatReady) {
        break
    }

    Write-Host (
        "Waiting... attempt {0}/{1} | api={2} | worker={3} | beat={4}" -f
        $attempt,
        $maxAttempts,
        ($(if ($isHealthy) { 'ready' } else { 'pending' })),
        ($(if ($workerReady) { 'ready' } else { 'pending' })),
        ($(if ($beatReady) { 'ready' } else { 'pending' }))
    ) -ForegroundColor DarkYellow
}

if (-not $isHealthy -or -not $workerReady -or -not $beatReady) {
    Write-Host "Startup check failed within 180 seconds." -ForegroundColor Red
    Write-Host "Check these commands:" -ForegroundColor Red
    Write-Host "1. docker compose -p vulnshield-iso logs -f api" -ForegroundColor Red
    Write-Host "2. docker compose -p vulnshield-iso logs -f worker" -ForegroundColor Red
    Write-Host "3. docker compose -p vulnshield-iso logs -f beat" -ForegroundColor Red
    Write-Host "4. verify your .env values" -ForegroundColor Red
    Show-StartupDiagnostics
    exit 1
}

Write-Host "API, worker, and beat are ready." -ForegroundColor Green

Write-Step "Step 4/5: Opening API docs..."
Start-Process "http://localhost:8000/docs"

Write-Step "Step 5/5: Printing next steps..."
Write-Host ""
Write-Host "---------------------------------------------------" -ForegroundColor White
Write-Host "System started." -ForegroundColor Green
Write-Host "Swagger:  http://localhost:8000/docs" -ForegroundColor White
Write-Host "Health:   http://localhost:8000/healthz" -ForegroundColor White
Write-Host "Use DEFAULT_ADMIN_USERNAME and DEFAULT_ADMIN_PASSWORD from .env to call /token first." -ForegroundColor White
Write-Host "---------------------------------------------------" -ForegroundColor White
Write-Host ""
