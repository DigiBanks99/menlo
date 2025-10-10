# Menlo Windows 10 + Podman Deployment Script
# Simplified deployment for Windows 10 Home using Podman containers

param(
    [Parameter(Mandatory = $true)]
    [string]$ImageTag,

    [Parameter(Mandatory = $false)]
    [string]$Registry = "ghcr.io",

    [Parameter(Mandatory = $false)]
    [string]$ImageName = "digibanks99/menlo/menlo-api",

    [Parameter(Mandatory = $false)]
    [string]$AppPath = "$env:USERPROFILE\menlo",

    [Parameter(Mandatory = $false)]
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Menlo deployment..." -ForegroundColor Green
Write-Host "Image: $Registry/$ImageName`:$ImageTag" -ForegroundColor Cyan
Write-Host "App Path: $AppPath" -ForegroundColor Cyan
Write-Host ""

# Ensure application directory exists
if (-not (Test-Path $AppPath)) {
    New-Item -ItemType Directory -Path $AppPath -Force | Out-Null
    New-Item -ItemType Directory -Path "$AppPath\backups" -Force | Out-Null
}

# Environment variables for deployment
$env:POSTGRES_USER = "menlo"
$env:POSTGRES_PASSWORD = "menlo_password_change_in_production"
$env:POSTGRES_DB = "menlo"

# Create docker-compose.prod.yml
Write-Host "📄 Creating docker-compose.prod.yml..." -ForegroundColor Yellow
$composeContent = @"
version: '3.8'

services:
  menlo-api:
    image: $Registry/$ImageName`:$ImageTag
    container_name: menlo-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=$env:POSTGRES_DB;Username=$env:POSTGRES_USER;Password=$env:POSTGRES_PASSWORD
      - Ollama__BaseUrl=http://ollama:11434
      - Logging__LogLevel__Default=Information
    ports:
      - "8080:8080"
    networks:
      - menlo-network
    depends_on:
      - postgres
      - ollama
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: postgres:17
    container_name: menlo-postgres
    restart: unless-stopped
    environment:
      - POSTGRES_DB=$env:POSTGRES_DB
      - POSTGRES_USER=$env:POSTGRES_USER
      - POSTGRES_PASSWORD=$env:POSTGRES_PASSWORD
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - menlo-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $env:POSTGRES_USER -d $env:POSTGRES_DB"]
      interval: 30s
      timeout: 10s
      retries: 5

  ollama:
    image: ollama/ollama:latest
    container_name: menlo-ollama
    restart: unless-stopped
    volumes:
      - ollama_data:/root/.ollama
    ports:
      - "11434:11434"
    networks:
      - menlo-network
    environment:
      - OLLAMA_KEEP_ALIVE=24h
      - OLLAMA_NUM_PARALLEL=2
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:11434/api/tags || exit 1"]
      interval: 60s
      timeout: 30s
      retries: 3
      start_period: 120s

volumes:
  postgres_data:
    name: menlo_postgres_data
  ollama_data:
    name: menlo_ollama_data

networks:
  menlo-network:
    name: menlo_network
    driver: bridge
"@

$composeFile = "$AppPath\docker-compose.prod.yml"
$composeContent | Out-File -FilePath $composeFile -Encoding UTF8

# Backup existing deployment
if (-not $SkipBackup -and (Test-Path $composeFile)) {
    Write-Host "💾 Creating backup..." -ForegroundColor Yellow
    $backupName = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $composeFile "$AppPath\backups\$backupName-docker-compose.yml"
    Write-Host "✅ Backup created: $backupName" -ForegroundColor Green
}

# Change to app directory
Set-Location $AppPath

# Pull new image
Write-Host "📥 Pulling image: $Registry/$ImageName`:$ImageTag" -ForegroundColor Yellow
podman pull "$Registry/$ImageName`:$ImageTag"

# Stop existing services
Write-Host "⏸️ Stopping existing services..." -ForegroundColor Yellow
podman-compose -f docker-compose.prod.yml down --remove-orphans 2>$null

# Start services
Write-Host "▶️ Starting services..." -ForegroundColor Yellow
podman-compose -f docker-compose.prod.yml up -d

# Wait for services to be healthy
Write-Host "🏥 Waiting for services to be healthy..." -ForegroundColor Yellow
$maxAttempts = 20
$attempt = 0

while ($attempt -lt $maxAttempts) {
    $attempt++
    Write-Host "Health check attempt $attempt/$maxAttempts..." -ForegroundColor Cyan

    Start-Sleep -Seconds 15

    # Check if containers are running
    $containers = podman-compose -f docker-compose.prod.yml ps --format json | ConvertFrom-Json
    $healthyCount = 0

    foreach ($container in $containers) {
        if ($container.State -eq "running") {
            $healthyCount++
        }
    }

    if ($healthyCount -eq 3) {
        Write-Host "✅ All services are running!" -ForegroundColor Green
        break
    }

    if ($attempt -eq $maxAttempts) {
        Write-Host "❌ Services failed to start properly" -ForegroundColor Red
        Write-Host "Container status:" -ForegroundColor Yellow
        podman-compose -f docker-compose.prod.yml ps
        exit 1
    }
}

# Test API health
Write-Host "🔍 Testing API health..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

for ($i = 1; $i -le 10; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ API health check passed!" -ForegroundColor Green
            break
        }
    } catch {
        if ($i -eq 10) {
            Write-Host "⚠️ API health check failed, but containers are running" -ForegroundColor Yellow
        } else {
            Write-Host "⏳ API not ready yet... ($i/10)" -ForegroundColor Cyan
            Start-Sleep -Seconds 10
        }
    }
}

# Setup Ollama models (background task)
Write-Host "🤖 Setting up Ollama models..." -ForegroundColor Yellow
Write-Host "📝 Note: Model download will continue in background. First run may take 10-30 minutes." -ForegroundColor Blue

Start-Job -ScriptBlock {
    Start-Sleep -Seconds 30  # Wait for Ollama to be ready

    # Pull required models
    podman exec menlo-ollama ollama pull phi4-mini 2>/dev/null
    podman exec menlo-ollama ollama pull phi4-vision 2>/dev/null

    # Fallback to smaller models if phi4 not available
    podman exec menlo-ollama ollama pull phi3.5:3.8b 2>/dev/null
    podman exec menlo-ollama ollama pull llava:latest 2>/dev/null
} | Out-Null

# Cleanup old images
Write-Host "🧹 Cleaning up old images..." -ForegroundColor Yellow
$oldImages = podman images "$Registry/$ImageName" --format "{{.Tag}}" | Where-Object { $_ -ne $ImageTag -and $_ -ne "latest" -and $_ -ne "<none>" }
foreach ($tag in $oldImages | Select-Object -Skip 2) {
    Write-Host "Removing old image: $Registry/$ImageName`:$tag" -ForegroundColor Gray
    podman rmi "$Registry/$ImageName`:$tag" 2>/dev/null
}

# Show final status
Write-Host ""
Write-Host "🎉 Deployment completed!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Service Status:" -ForegroundColor Cyan
podman-compose -f docker-compose.prod.yml ps

Write-Host ""
Write-Host "🌐 Application URLs:" -ForegroundColor Cyan
Write-Host "  API Health: http://localhost:8080/health" -ForegroundColor White
Write-Host "  API Docs: http://localhost:8080/swagger" -ForegroundColor White
Write-Host "  Ollama API: http://localhost:11434" -ForegroundColor White
Write-Host "  PostgreSQL: localhost:5432 (user: $env:POSTGRES_USER)" -ForegroundColor White

Write-Host ""
Write-Host "� Useful Commands:" -ForegroundColor Cyan
Write-Host "  View logs: podman-compose -f docker-compose.prod.yml logs -f" -ForegroundColor White
Write-Host "  Stop services: podman-compose -f docker-compose.prod.yml down" -ForegroundColor White
Write-Host "  Restart: podman-compose -f docker-compose.prod.yml restart" -ForegroundColor White
Write-Host "  Check models: podman exec menlo-ollama ollama list" -ForegroundColor White
