# Windows Home Server Setup for Menlo

## 🖥️ Windows 10 Home Deployment Guide

This guide adapts the Menlo deployment for Windows 10 Home, working around the limitations of the Home edition.

### 📋 Prerequisites

1. **Windows 10 Home** (version 1903 or later)
2. **WSL2 enabled** (Windows Subsystem for Linux 2)
3. **Docker running in WSL2**
4. **OpenSSH Server** (for GitHub Actions deployment)
5. **PowerShell 7** (recommended)

## 🛠️ Setup Steps

### Step 1: Enable WSL2 and Install Ubuntu

Open PowerShell as Administrator:

```powershell
# Enable WSL2
wsl --install

# Restart computer when prompted
# After restart, install Ubuntu
wsl --install -d Ubuntu
```

### Step 2: Install Podman in WSL2

Open Ubuntu WSL2 terminal:

```bash
# Update packages
sudo apt update && sudo apt upgrade -y

# Install Podman
sudo apt install -y podman podman-compose

# Alternative: Install latest Podman from official repository
# . /etc/os-release
# sudo mkdir -p /etc/apt/keyrings
# curl -fsSL https://download.opensuse.org/repositories/devel:kubic:libcontainers:unstable/xUbuntu_${VERSION_ID}/Release.key \
#   | gpg --dearmor \
#   | sudo tee /etc/apt/keyrings/devel_kubic_libcontainers_unstable.gpg > /dev/null
# echo \
#   "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/devel_kubic_libcontainers_unstable.gpg]\
#     https://download.opensuse.org/repositories/devel:kubic:libcontainers:unstable/xUbuntu_${VERSION_ID}/ /" \
#   | sudo tee /etc/apt/sources.list.d/devel:kubic:libcontainers:unstable.list > /dev/null
# sudo apt update
# sudo apt install -y podman podman-compose

# Configure Podman for rootless operation (automatically enabled)
podman system migrate

# Test Podman installation
podman --version
podman-compose --version

# Create systemd user directory for auto-start
mkdir -p ~/.config/systemd/user
```

### Step 3: Install OpenSSH Server on Windows

Open PowerShell as Administrator:

```powershell
# Add OpenSSH Server capability
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start and enable SSH service
Start-Service sshd
Set-Service -Name sshd -StartupType 'Automatic'

# Configure firewall rule (if needed)
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

### Step 4: Create Menlo User and Directory

In PowerShell as Administrator:

```powershell
# Create menlo user
net user menlo P@ssw0rd123! /add /comment:"Menlo Application User"
net localgroup "Users" menlo /add

# Create application directory
mkdir C:\menlo
mkdir C:\menlo\backups
mkdir C:\menlo\logs

# Set permissions (allow menlo user access)
icacls C:\menlo /grant "menlo:(OI)(CI)F"
```

### Step 5: Configure SSH Key Authentication

1. **Generate SSH key pair** on your development machine:
   ```bash
   ssh-keygen -t rsa -b 4096 -f ~/.ssh/menlo_deploy_key
   ```

2. **Copy public key to Windows server**:
   ```powershell
   # On Windows server, create SSH directory for menlo user
   mkdir C:\Users\menlo\.ssh
   
   # Copy the public key content to authorized_keys file
   # (paste the content of menlo_deploy_key.pub)
   notepad C:\Users\menlo\.ssh\authorized_keys
   
   # Set proper permissions
   icacls C:\Users\menlo\.ssh /inheritance:r
   icacls C:\Users\menlo\.ssh /grant "menlo:(OI)(CI)F"
   icacls C:\Users\menlo\.ssh\authorized_keys /inheritance:r
   icacls C:\Users\menlo\.ssh\authorized_keys /grant "menlo:R"
   ```

## 🐳 Podman Deployment Scripts for Windows

### Windows PowerShell Deployment Script

Create `C:\menlo\deploy.ps1`:

```powershell
# Menlo Windows Deployment Script with Podman
param(
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Menlo deployment with Podman..." -ForegroundColor Green

# Configuration
$ComposeFile = "docker-compose.prod.yml"
$AppDir = "C:\menlo"
$BackupDir = "C:\menlo\backups"

# Ensure directories exist
if (!(Test-Path $BackupDir)) {
    New-Item -Path $BackupDir -ItemType Directory -Force
}

Set-Location $AppDir

# Backup current configuration
if (Test-Path $ComposeFile) {
    Write-Host "📦 Backing up current configuration..." -ForegroundColor Yellow
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    Copy-Item $ComposeFile "$BackupDir\docker-compose.$timestamp.yml"
}

# Use WSL2 to run Podman commands
Write-Host "📥 Pulling new container images..." -ForegroundColor Yellow
wsl -d Ubuntu podman-compose -f $ComposeFile pull

# Perform database backup if Postgres is running
Write-Host "💾 Creating database backup..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
try {
    wsl -d Ubuntu docker-compose -f $ComposeFile exec -T postgres pg_dump -U $env:POSTGRES_USER $env:POSTGRES_DB > "$BackupDir\menlo_db_$timestamp.sql"
} catch {
    Write-Host "⚠️ Database backup failed" -ForegroundColor Yellow
}

# Deploy with zero-downtime strategy
Write-Host "🔄 Deploying new version..." -ForegroundColor Yellow
wsl -d Ubuntu docker-compose -f $ComposeFile up -d --remove-orphans

# Wait for health checks
Write-Host "🏥 Waiting for health checks..." -ForegroundColor Yellow
for ($i = 1; $i -le 30; $i++) {
    $healthStatus = wsl -d Ubuntu docker-compose -f $ComposeFile ps --format "table {{.Service}}\t{{.Status}}"
    if ($healthStatus -match "healthy|Up \(healthy\)") {
        Write-Host "✅ Services are healthy" -ForegroundColor Green
        break
    }
    Write-Host "⏳ Waiting for services to be healthy... ($i/30)" -ForegroundColor Yellow
    Start-Sleep 10
}

# Clean up old images
Write-Host "🧹 Cleaning up old container images..." -ForegroundColor Yellow
wsl -d Ubuntu docker image prune -f

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green

# Display running services
Write-Host "📊 Running services:" -ForegroundColor Cyan
wsl -d Ubuntu podman-compose -f $ComposeFile ps
```

### Health Check Script

Create `C:\menlo\healthcheck.ps1`:

```powershell
# Menlo Health Check Script with Podman
$ErrorActionPreference = "SilentlyContinue"

Write-Host "🏥 Menlo Health Check" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan

# Check API
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API is healthy" -ForegroundColor Green
    } else {
        Write-Host "❌ API is unhealthy (Status: $($response.StatusCode))" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ API is unhealthy (Error: $($_.Exception.Message))" -ForegroundColor Red
}

# Check database
try {
    $dbCheck = wsl -d Ubuntu podman-compose -f C:\menlo\docker-compose.prod.yml exec -T postgres pg_isready -U $env:POSTGRES_USER -d $env:POSTGRES_DB
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database is healthy" -ForegroundColor Green
    } else {
        Write-Host "❌ Database is unhealthy" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Database check failed" -ForegroundColor Red
}

# Check Ollama
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Ollama is healthy" -ForegroundColor Green
    } else {
        Write-Host "❌ Ollama is unhealthy" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ollama is unhealthy" -ForegroundColor Red
}

Write-Host "Health check completed" -ForegroundColor Cyan
```

## � PowerShell Deployment Scripts (Windows-specific)

Since GitHub Actions will connect to your Windows machine via SSH but execute commands in WSL2, here are Windows-specific scripts:

### 1. Discover Your Server IP Address

Run this script to find your HOME_SERVER_HOST value:

```powershell
.\deployment\discover-host.ps1
```

This will show you:

- All active network interfaces and their IP addresses  
- Recommended IP address for HOME_SERVER_HOST
- WSL2 configuration status
- Internet connectivity test results

### 2. Test Deployment Locally

**deployment/deploy-windows.ps1** (for local testing):

```powershell
# Test deployment locally
.\deployment\deploy-windows.ps1 -ImageTag "latest" -ImageName "your-username/menlo-api"

# With custom registry
.\deployment\deploy-windows.ps1 -ImageTag "v1.0.0" -ImageName "ghcr.io/your-username/menlo-api" -Registry "ghcr.io"

# Skip backup during testing
.\deployment\deploy-windows.ps1 -ImageTag "latest" -ImageName "your-username/menlo-api" -SkipBackup
```

**SSH Command Adaptations:**

- Use `wsl` prefix for Podman commands: `wsl podman ps`  
- Use `wsl` for file operations: `wsl mkdir -p /path`
- PowerShell variables work with WSL: `wsl echo $env:VARIABLE`
- Podman-compose instead of docker-compose: `wsl podman-compose up -d`

## �🔧 GitHub Actions Configuration for Windows

You'll need to update your GitHub secrets with:

### Finding Your HOME_SERVER_HOST

Use the discover-host.ps1 script above, or manually check:

```powershell
# Get your local IP address
Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like "192.168.*" -or $_.IPAddress -like "10.0.*"}
```

Example output: `192.168.1.100`

### GitHub Secrets for Windows

```bash
HOME_SERVER_HOST=192.168.1.100     # Your Windows machine's local IP
HOME_SERVER_USER=menlo             # The user we created
HOME_SERVER_SSH_KEY=               # Contents of your private key (menlo_deploy_key)
POSTGRES_USER=menlo_user
POSTGRES_PASSWORD=YourSecurePassword123!
DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=menlo;Username=menlo_user;Password=YourSecurePassword123!
```

## 🚨 Important Windows Considerations

### 1. **WSL2 Integration**

- Podman runs inside WSL2, not natively on Windows
- File paths need to be accessible from WSL2
- Use `wsl -d Ubuntu podman-compose ...` commands
- Rootless containers provide better security than Docker

### 2. **Firewall Configuration**

- Windows Defender Firewall may block Podman ports
- Configure rules for ports 8080, 5432, 11434 if needed
- Podman rootless mode may require different port handling

### 3. **Automatic Startup**

- WSL2 doesn't auto-start on boot
- Consider creating a Windows service or startup script

### 4. **Performance**

- WSL2 may have slight performance overhead
- File I/O between Windows and WSL2 can be slower
- Keep Podman volumes in WSL2 filesystem for better performance
- Rootless Podman may have slightly different performance characteristics

### 5. **Backup Strategy**

- Windows paths for backups: `C:\menlo\backups\`
- Consider Windows Task Scheduler for automated backups

## 🎯 Next Steps

1. **Set up WSL2 and Podman** following the steps above
2. **Configure SSH access** with key-based authentication
3. **Find your local IP address** using the PowerShell command
4. **Update GitHub secrets** with your Windows server details
5. **Test deployment** by running the health check script

This Windows setup maintains the same hybrid architecture benefits while working within Windows 10 Home limitations, with the added security of rootless Podman containers!