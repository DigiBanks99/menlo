# Menlo Windows 10 + Podman Setup Script
# This script sets up a complete Windows 10 development and deployment environment
# for the Menlo Home Management application using Podman containers

param(
    [Parameter(Mandatory = $false)]
    [string]$Domain = "yourdomain.com",

    [Parameter(Mandatory = $false)]
    [string]$Subdomain = "api.menlo",

    [Parameter(Mandatory = $false)]
    [switch]$SkipWSL2Install,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPodmanInstall,

    [Parameter(Mandatory = $false)]
    [switch]$SkipCloudflaredInstall,

    [Parameter(Mandatory = $false)]
    [switch]$SkipSSHSetup
)

$ErrorActionPreference = "Stop"

Write-Host "🏠 Menlo Home Setup" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Running as Administrator" -ForegroundColor Green

# Function to check if a command exists
function Test-CommandExists {
    param($command)
    try {
        Get-Command $command -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Function to install via winget
function Install-WingetPackage {
    param($PackageId, $PackageName)

    Write-Host "📦 Installing $PackageName..." -ForegroundColor Yellow
    try {
        winget install --id $PackageId --silent --accept-package-agreements --accept-source-agreements
        Write-Host "✅ $PackageName installed successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to install $PackageName" -ForegroundColor Red
        throw
    }
}

# Step 1: Install WSL2 if needed
if (-not $SkipWSL2Install) {
    Write-Host ""
    Write-Host "🐧 Setting up WSL2..." -ForegroundColor Cyan

    # Check if WSL is already installed
    if (Test-CommandExists "wsl") {
        Write-Host "✅ WSL is already installed" -ForegroundColor Green

        # Check WSL version
        $wslVersion = wsl --status 2>$null | Select-String "Default Version"
        if ($wslVersion -match "2") {
            Write-Host "✅ WSL2 is the default version" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Setting WSL2 as default version..." -ForegroundColor Yellow
            wsl --set-default-version 2
        }
    } else {
        Write-Host "📥 Installing WSL2..." -ForegroundColor Yellow

        # Enable WSL feature
        dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
        dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

        # Install WSL
        wsl --install --no-launch

        Write-Host "⚠️ WSL2 requires a system restart. Please restart and run this script again." -ForegroundColor Yellow
        Write-Host "After restart, run: setup-windows-podman.ps1 -SkipWSL2Install" -ForegroundColor Yellow
        exit 0
    }
} else {
    Write-Host "⏭️ Skipping WSL2 installation" -ForegroundColor Blue
}

# Step 2: Install Podman Desktop
if (-not $SkipPodmanInstall) {
    Write-Host ""
    Write-Host "🐳 Setting up Podman..." -ForegroundColor Cyan

    if (Test-CommandExists "podman") {
        Write-Host "✅ Podman is already installed" -ForegroundColor Green
    } else {
        Install-WingetPackage "RedHat.Podman-Desktop" "Podman Desktop"

        # Add Podman to PATH (Podman Desktop usually handles this)
        $podmanPath = "${env:ProgramFiles}\RedHat\Podman\bin"
        if (Test-Path $podmanPath) {
            $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
            if ($currentPath -notlike "*$podmanPath*") {
                [Environment]::SetEnvironmentVariable("Path", "$currentPath;$podmanPath", "Machine")
                $env:Path += ";$podmanPath"
            }
        }
    }

    # Initialize Podman machine if needed
    Write-Host "⚙️ Initializing Podman machine..." -ForegroundColor Yellow
    try {
        # Check if machine exists
        $machines = podman machine list --format json 2>$null | ConvertFrom-Json
        if (-not $machines -or $machines.Count -eq 0) {
            Write-Host "Creating new Podman machine..." -ForegroundColor Yellow
            podman machine init --cpus 4 --memory 8192 --disk-size 100
        }

        # Start machine if not running
        $machineStatus = podman machine list --format "{{.Running}}" 2>$null
        if ($machineStatus -notcontains "true") {
            Write-Host "Starting Podman machine..." -ForegroundColor Yellow
            podman machine start
        }

        Write-Host "✅ Podman machine is running" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Podman machine setup had issues, but continuing..." -ForegroundColor Yellow
    }

    # Install podman-compose
    Write-Host "📦 Installing podman-compose..." -ForegroundColor Yellow
    if (Test-CommandExists "pip3") {
        pip3 install podman-compose
    } elseif (Test-CommandExists "pip") {
        pip install podman-compose
    } else {
        Write-Host "⚠️ Python pip not found. Installing Python..." -ForegroundColor Yellow
        Install-WingetPackage "Python.Python.3.12" "Python 3.12"
        # Refresh PATH
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        pip install podman-compose
    }

} else {
    Write-Host "⏭️ Skipping Podman installation" -ForegroundColor Blue
}

# Step 3: Install Cloudflare Tunnel
if (-not $SkipCloudflaredInstall) {
    Write-Host ""
    Write-Host "☁️ Setting up Cloudflare Tunnel..." -ForegroundColor Cyan

    if (Test-CommandExists "cloudflared") {
        Write-Host "✅ Cloudflared is already installed" -ForegroundColor Green
    } else {
        # Download and install cloudflared
        $cloudflaredUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
        $cloudflaredPath = "${env:ProgramFiles}\cloudflared"
        $cloudflaredExe = "$cloudflaredPath\cloudflared.exe"

        Write-Host "📥 Downloading cloudflared..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $cloudflaredPath -Force | Out-Null
        Invoke-WebRequest -Uri $cloudflaredUrl -OutFile $cloudflaredExe

        # Add to PATH
        $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($currentPath -notlike "*$cloudflaredPath*") {
            [Environment]::SetEnvironmentVariable("Path", "$currentPath;$cloudflaredPath", "Machine")
            $env:Path += ";$cloudflaredPath"
        }

        Write-Host "✅ Cloudflared installed successfully" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "🔗 Cloudflare Tunnel Setup Instructions:" -ForegroundColor Yellow
    Write-Host "1. Run: cloudflared tunnel login" -ForegroundColor White
    Write-Host "2. Run: cloudflared tunnel create menlo-api" -ForegroundColor White
    Write-Host "3. Configure DNS: cloudflared tunnel route dns menlo-api $Subdomain.$Domain" -ForegroundColor White
    Write-Host "4. Create config file at ~/.cloudflared/config.yml with hostname: $Subdomain.$Domain" -ForegroundColor White
    Write-Host "5. Run: cloudflared service install" -ForegroundColor White

} else {
    Write-Host "⏭️ Skipping Cloudflared installation" -ForegroundColor Blue
}

# Step 4: SSH Setup for GitHub Actions
if (-not $SkipSSHSetup) {
    Write-Host ""
    Write-Host "🔑 Setting up SSH for GitHub Actions..." -ForegroundColor Cyan

    # Check if OpenSSH Server is installed using Get-Service (more reliable)
    try {
        $sshService = Get-Service -Name sshd -ErrorAction SilentlyContinue
        if (-not $sshService) {
            Write-Host "📦 Installing OpenSSH Server..." -ForegroundColor Yellow

            # Use winget to install OpenSSH (more reliable than Windows capabilities)
            try {
                winget install --id Microsoft.OpenSSH.Beta --silent --accept-package-agreements --accept-source-agreements
                Write-Host "✅ OpenSSH Server installed via winget" -ForegroundColor Green
            } catch {
                Write-Host "⚠️ Winget install failed, trying Windows optional features..." -ForegroundColor Yellow

                # Fallback to DISM command line tool
                $dismResult = dism /online /add-capability /capabilityname:OpenSSH.Server~~~~0.0.1.0
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ OpenSSH Server installed via DISM" -ForegroundColor Green
                } else {
                    Write-Host "❌ Failed to install OpenSSH Server. Please install manually from Windows Settings > Apps > Optional Features" -ForegroundColor Red
                    Write-Host "   Or download from: https://github.com/PowerShell/Win32-OpenSSH/releases" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "✅ OpenSSH Server is already installed" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️ Could not determine OpenSSH Server status" -ForegroundColor Yellow
    }

    # Start and enable SSH service (if it exists)
    try {
        Set-Service -Name sshd -StartupType 'Automatic' -ErrorAction SilentlyContinue
        Start-Service sshd -ErrorAction SilentlyContinue
        Write-Host "✅ SSH service configured and started" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Could not start SSH service. You may need to restart after OpenSSH installation." -ForegroundColor Yellow
    }

    # Configure firewall
    if (!(Get-NetFirewallRule -DisplayName "OpenSSH-Server-In-TCP" -ErrorAction SilentlyContinue)) {
        Write-Host "🔥 Configuring Windows Firewall for SSH..." -ForegroundColor Yellow
        New-NetFirewallRule -DisplayName 'OpenSSH-Server-In-TCP' -Direction Inbound -Protocol TCP -LocalPort 22 -Action Allow
    }

    Write-Host "✅ SSH Server is configured and running" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔐 SSH Setup Instructions:" -ForegroundColor Yellow
    Write-Host "1. Generate SSH key pair for GitHub Actions:" -ForegroundColor White
    Write-Host "   ssh-keygen -t ed25519 -f ~/.ssh/menlo_deploy" -ForegroundColor Gray
    Write-Host "2. Add public key to ~/.ssh/authorized_keys" -ForegroundColor White
    Write-Host "3. Add private key to GitHub repository secrets as HOME_SERVER_SSH_KEY" -ForegroundColor White

} else {
    Write-Host "⏭️ Skipping SSH setup" -ForegroundColor Blue
}

# Step 5: Create Menlo application directory
Write-Host ""
Write-Host "📁 Setting up Menlo application directory..." -ForegroundColor Cyan

$menloPath = "$env:USERPROFILE\menlo"
New-Item -ItemType Directory -Path $menloPath -Force | Out-Null
New-Item -ItemType Directory -Path "$menloPath\backups" -Force | Out-Null

Write-Host "✅ Created application directory: $menloPath" -ForegroundColor Green

# Step 6: Validation
Write-Host ""
Write-Host "🔍 Validating installation..." -ForegroundColor Cyan

$validationPassed = $true

# Check WSL2
if (Test-CommandExists "wsl") {
    Write-Host "✅ WSL2: Available" -ForegroundColor Green
} else {
    Write-Host "❌ WSL2: Not available" -ForegroundColor Red
    $validationPassed = $false
}

# Check Podman
if (Test-CommandExists "podman") {
    try {
        $podmanVersion = podman --version
        Write-Host "✅ Podman: $podmanVersion" -ForegroundColor Green
    } catch {
        Write-Host "❌ Podman: Not working properly" -ForegroundColor Red
        $validationPassed = $false
    }
} else {
    Write-Host "❌ Podman: Not available" -ForegroundColor Red
    $validationPassed = $false
}

# Check podman-compose
if (Test-CommandExists "podman-compose") {
    Write-Host "✅ podman-compose: Available" -ForegroundColor Green
} else {
    Write-Host "❌ podman-compose: Not available" -ForegroundColor Red
    $validationPassed = $false
}

# Check Cloudflared
if (Test-CommandExists "cloudflared") {
    try {
        $cloudflaredVersion = cloudflared --version
        Write-Host "✅ Cloudflared: $cloudflaredVersion" -ForegroundColor Green
    } catch {
        Write-Host "❌ Cloudflared: Not working properly" -ForegroundColor Red
        $validationPassed = $false
    }
} else {
    Write-Host "❌ Cloudflared: Not available" -ForegroundColor Red
    $validationPassed = $false
}

# Check SSH
try {
    $sshStatus = Get-Service sshd -ErrorAction Stop
    if ($sshStatus.Status -eq "Running") {
        Write-Host "✅ SSH Server: Running" -ForegroundColor Green
    } else {
        Write-Host "⚠️ SSH Server: Not running" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ SSH Server: Not available" -ForegroundColor Red
    $validationPassed = $false
}

Write-Host ""
if ($validationPassed) {
    Write-Host "🎉 Setup completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Configure Cloudflare Tunnel (see instructions above)" -ForegroundColor White
    Write-Host "2. Set up SSH keys for GitHub Actions (see instructions above)" -ForegroundColor White
    Write-Host "3. Run discover-host.ps1 to find your server IP" -ForegroundColor White
    Write-Host "4. Configure GitHub repository secrets" -ForegroundColor White
    Write-Host "5. Deploy your application using GitHub Actions" -ForegroundColor White
} else {
    Write-Host "⚠️ Setup completed with some issues. Please review the errors above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🔗 Useful Commands:" -ForegroundColor Cyan
Write-Host "  Check Podman: podman --version" -ForegroundColor White
Write-Host "  Start Podman: podman machine start" -ForegroundColor White
Write-Host "  Test deployment: .\deploy-windows.ps1 -ImageTag latest -ImageName test/app" -ForegroundColor White
Write-Host "  Find your IP: .\discover-host.ps1" -ForegroundColor White
Write-Host "  Your API URL will be: https://$Subdomain.$Domain" -ForegroundColor Yellow
