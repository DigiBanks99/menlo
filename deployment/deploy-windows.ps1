# Windows PowerShell Deployment Script for Menlo with Podman
# This script handles the same deployment logic as the GitHub Actions workflow
# but can be run locally for testing using Podman instead of Docker

param(
    [Parameter(Mandatory = $true)]
    [string]$ImageTag,
    
    [Parameter(Mandatory = $false)]
    [string]$Registry = "ghcr.io",
    
    [Parameter(Mandatory = $false)]
    [string]$ImageName = "your-username/menlo-api",
    
    [Parameter(Mandatory = $false)]
    [string]$DeploymentPath = "/home/menlo/deployment",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Menlo deployment with Podman..." -ForegroundColor Green
Write-Host "Image: $Registry/$ImageName`:$ImageTag" -ForegroundColor Cyan

# Create deployment directory structure
Write-Host "📁 Creating deployment directories..." -ForegroundColor Yellow
wsl mkdir -p $DeploymentPath/backup

# Generate docker-compose.prod.yml
Write-Host "📄 Generating docker-compose.prod.yml..." -ForegroundColor Yellow
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
      - ConnectionStrings__DefaultConnection=`${DATABASE_CONNECTION_STRING}
      - Ollama__BaseUrl=http://ollama:11434
      - Logging__LogLevel__Default=Information
      - HealthChecks__Enabled=true
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
      - POSTGRES_DB=menlo
      - POSTGRES_USER=`${POSTGRES_USER}
      - POSTGRES_PASSWORD=`${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - menlo-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U `${POSTGRES_USER} -d menlo"]
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

$composeContent | wsl tee "$DeploymentPath/docker-compose.prod.yml" > $null

# Backup current deployment (if not skipped)
if (-not $SkipBackup) {
    Write-Host "💾 Creating backup..." -ForegroundColor Yellow
    $backupName = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    # Check if current compose file exists
    $composeExists = wsl test -f "$DeploymentPath/docker-compose.prod.yml" -and echo "true" -or echo "false"
    if ($composeExists -eq "true") {
        wsl cp "$DeploymentPath/docker-compose.prod.yml" "$DeploymentPath/backup/$backupName-docker-compose.yml"
        Write-Host "✅ Backup created: $backupName" -ForegroundColor Green
    } else {
        Write-Host "ℹ️ No existing deployment to backup" -ForegroundColor Blue
    }
}

# Pull the new image
Write-Host "📥 Pulling new image..." -ForegroundColor Yellow
wsl podman pull "$Registry/$ImageName`:$ImageTag"

# Deploy with zero-downtime strategy
Write-Host "🔄 Deploying with zero-downtime strategy..." -ForegroundColor Yellow

# Check if services are already running
$runningContainers = wsl podman ps --filter "name=menlo-" --format "`"{{.Names}}`""
if ($runningContainers) {
    Write-Host "⏸️ Stopping existing services..." -ForegroundColor Yellow
    wsl podman-compose -f "$DeploymentPath/docker-compose.prod.yml" down --remove-orphans
    Start-Sleep -Seconds 5
}

# Start new services
Write-Host "▶️ Starting new services..." -ForegroundColor Yellow
wsl podman-compose -f "$DeploymentPath/docker-compose.prod.yml" up -d

# Wait for services to be healthy
Write-Host "🏥 Waiting for services to be healthy..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$allHealthy = $false

while ($attempt -lt $maxAttempts -and -not $allHealthy) {
    $attempt++
    Write-Host "Health check attempt $attempt/$maxAttempts..." -ForegroundColor Cyan
    
    # Check API health
    $apiHealth = wsl podman exec menlo-api curl -sf http://localhost:8080/health 2>/dev/null
    $postgresHealth = wsl podman exec menlo-postgres pg_isready -U "`${POSTGRES_USER}" -d menlo 2>/dev/null
    $ollamaHealth = wsl podman exec menlo-ollama curl -sf http://localhost:11434/api/tags 2>/dev/null
    
    if ($apiHealth -and $postgresHealth -and $ollamaHealth) {
        $allHealthy = $true
        Write-Host "✅ All services are healthy!" -ForegroundColor Green
    } else {
        Start-Sleep -Seconds 10
    }
}

if (-not $allHealthy) {
    Write-Host "❌ Services failed to become healthy within timeout" -ForegroundColor Red
    Write-Host "Checking container logs..." -ForegroundColor Yellow
    wsl podman-compose -f "$DeploymentPath/docker-compose.prod.yml" logs --tail=50
    exit 1
}

# Cleanup old images (keep last 3 versions)
Write-Host "🧹 Cleaning up old images..." -ForegroundColor Yellow
$oldImages = wsl podman images "$Registry/$ImageName" --format "`"{{.Tag}}`"" | Select-Object -Skip 3
if ($oldImages) {
    foreach ($tag in $oldImages) {
        if ($tag -ne "latest" -and $tag -ne "<none>") {
            Write-Host "Removing old image: $Registry/$ImageName`:$tag" -ForegroundColor Gray
            wsl podman rmi "$Registry/$ImageName`:$tag" 2>/dev/null
        }
    }
}

# Show deployment status
Write-Host ""
Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
Write-Host "📊 Service Status:" -ForegroundColor Cyan
wsl podman-compose -f "$DeploymentPath/docker-compose.prod.yml" ps

Write-Host ""
Write-Host "🌐 Application URLs:" -ForegroundColor Cyan
Write-Host "  API: http://localhost:8080" -ForegroundColor White
Write-Host "  Health: http://localhost:8080/health" -ForegroundColor White
Write-Host "  Swagger: http://localhost:8080/swagger" -ForegroundColor White
Write-Host "  Ollama: http://localhost:11434" -ForegroundColor White
Write-Host ""
Write-Host "📚 Useful commands:" -ForegroundColor Cyan
Write-Host "  View logs: wsl podman-compose -f `"$DeploymentPath/docker-compose.prod.yml`" logs -f" -ForegroundColor White
Write-Host "  Stop services: wsl podman-compose -f `"$DeploymentPath/docker-compose.prod.yml`" down" -ForegroundColor White
Write-Host "  Restart services: wsl podman-compose -f `"$DeploymentPath/docker-compose.prod.yml`" restart" -ForegroundColor White
