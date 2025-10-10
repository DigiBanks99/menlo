# Menlo Deployment - Windows 10 + Podman + Ollama

Streamlined deployment guide for the Menlo Home Management application on **Windows 10 Home** using **Podman containers**, **Ollama AI**, and **Cloudflare Tunnels**.

## 🎯 Quick Start

This setup is designed specifically for:

- **Windows 10 Home** (primary development/server environment)
- **Podman containers** (rootless, secure container runtime)
- **Local Ollama AI** (phi4-mini, phi4-vision models)
- **Cloudflare Tunnels** (secure remote access without exposing IPs)

## 📁 Files Overview

| File | Purpose |
|------|---------|
| `setup-windows-podman.ps1` | Complete Windows 10 + Podman setup automation |
| `deploy-windows.ps1` | Deployment script for local/remote deployment |
| `discover-host.ps1` | Network discovery for GitHub Actions setup |
| `README.md` | This guide |

## 🚀 Initial Setup

### Prerequisites

- Windows 10 Home (build 19041 or later)
- PowerShell 5.1 or later
- Administrator access
- Stable internet connection (for model downloads)

### Step 1: Run Setup Script

**Open PowerShell as Administrator** and run:

```powershell
cd deployment

# Basic setup with default domain (yourdomain.com)
.\setup-windows-podman.ps1

# Or specify your actual domain
.\setup-windows-podman.ps1 -Domain "example.com" -Subdomain "api.menlo"

# Example with your own domain
.\setup-windows-podman.ps1 -Domain "mydomain.co.za" -Subdomain "menlo-api"
```

This script will:

- ✅ Install/configure WSL2
- ✅ Install Podman Desktop
- ✅ Install podman-compose
- ✅ Install Cloudflare tunnel client
- ✅ Configure SSH server for GitHub Actions
- ✅ Create application directories
- ✅ Validate the complete setup

> **Note:** If WSL2 is not installed, the script will require a restart. Run the script again after restart with `-SkipWSL2Install`.

### Step 2: Configure Cloudflare Tunnel

Replace `YOUR_DOMAIN` and `YOUR_SUBDOMAIN` with your actual values:

```powershell
# Set your domain variables
$Domain = "example.com"  # Replace with your domain
$Subdomain = "api.menlo"  # Or your preferred subdomain

# Login to Cloudflare (opens browser)
cloudflared tunnel login

# Create tunnel for your API
cloudflared tunnel create menlo-api

# Configure DNS (use your actual domain)
cloudflared tunnel route dns menlo-api "$Subdomain.$Domain"

# Create config file
mkdir $env:USERPROFILE\.cloudflared
@"
tunnel: menlo-api
credentials-file: $env:USERPROFILE\.cloudflared\menlo-api.json

ingress:
  - hostname: $Subdomain.$Domain
    service: http://localhost:8080
  - service: http_status:404
"@ | Out-File -FilePath "$env:USERPROFILE\.cloudflared\config.yml" -Encoding UTF8

# Install as Windows service
cloudflared service install
```

### Step 3: Configure GitHub Actions

```powershell
# Find your server IP
.\discover-host.ps1

# Generate SSH key for GitHub Actions
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\menlo_deploy -N ""

# Add public key to authorized_keys
Add-Content -Path "$env:USERPROFILE\.ssh\authorized_keys" -Value (Get-Content "$env:USERPROFILE\.ssh\menlo_deploy.pub")
```

**Add these secrets to your GitHub repository:**

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `HOME_SERVER_HOST` | Your IP from discover-host.ps1 | Server IP address |
| `HOME_SERVER_USER` | Your Windows username | SSH user |
| `HOME_SERVER_SSH_KEY` | Private key content | Content of menlo_deploy file |
| `POSTGRES_USER` | `menlo` | Database user |
| `POSTGRES_PASSWORD` | `secure_password_here` | Database password |
| `API_DOMAIN` | `example.com` | Your domain name |
| `API_SUBDOMAIN` | `api.menlo` | Your API subdomain |

## � Deployment

### Local Deployment (Testing)

```powershell
# Test with a sample image
.\deploy-windows.ps1 -ImageTag "latest" -ImageName "hello-world"

# Deploy from GitHub Container Registry
.\deploy-windows.ps1 -ImageTag "main-abc123" -ImageName "digibanks99/menlo/menlo-api"
```

### Production Deployment (via GitHub Actions)

Push to the `main` branch or manually trigger the deployment workflow. The GitHub Actions will:

1. Build and push container image
2. Deploy to your Home via SSH
3. Verify health checks
4. Update Cloudflare tunnel (if configured)

## 🏥 Health Monitoring

### Check Service Status

```powershell
cd $env:USERPROFILE\menlo
podman-compose -f docker-compose.prod.yml ps
```

### View Logs

```powershell
# All services
podman-compose -f docker-compose.prod.yml logs -f

# Specific service
podman-compose -f docker-compose.prod.yml logs -f menlo-api
podman-compose -f docker-compose.prod.yml logs -f menlo-ollama
```

### Health Check URLs

- **API Health**: <http://localhost:8080/health>
- **API Documentation**: <http://localhost:8080/swagger>
- **Ollama API**: <http://localhost:11434/api/tags>
- **External (via Cloudflare)**: `https://YOUR_SUBDOMAIN.YOUR_DOMAIN/health`

> Replace `YOUR_SUBDOMAIN.YOUR_DOMAIN` with your actual configured domain (e.g., `https://api.menlo.example.com/health`)

## 🤖 AI Models Management

### Check Downloaded Models

```powershell
podman exec menlo-ollama ollama list
```

### Download Additional Models

```powershell
# Text processing models
podman exec menlo-ollama ollama pull phi4-mini
podman exec menlo-ollama ollama pull phi3.5:3.8b

# Vision models (image analysis)
podman exec menlo-ollama ollama pull phi4-vision
podman exec menlo-ollama ollama pull llava:latest
```

### Model Storage

Models are stored in the `ollama_data` volume and persist across container restarts.

## 🛠️ Troubleshooting

### Podman Issues

```powershell
# Restart Podman machine
podman machine stop
podman machine start

# Check Podman status
podman machine list
podman --version
```

### Database Issues

```powershell
# Reset database (WARNING: destroys data)
podman volume rm menlo_postgres_data
.\deploy-windows.ps1 -ImageTag "latest" -ImageName "your-image"

# Check database connection
podman exec menlo-postgres pg_isready -U menlo -d menlo
```

### Ollama Model Issues

```powershell
# Check Ollama status
curl http://localhost:11434/api/tags

# Restart Ollama container
podman restart menlo-ollama

# Clear models and re-download
podman volume rm menlo_ollama_data
podman restart menlo-ollama
```

### Network/Cloudflare Issues

```powershell
# Check tunnel status
cloudflared tunnel info menlo-api

# Restart tunnel service
Restart-Service cloudflared

# Test local connectivity
curl http://localhost:8080/health

# Test external connectivity (replace with your domain)
curl https://YOUR_SUBDOMAIN.YOUR_DOMAIN/health
```

## 📊 Resource Requirements

### Minimum System Requirements

- **CPU**: 4 cores (Intel i5 or AMD equivalent)
- **RAM**: 16GB (8GB for models + 8GB for OS/apps)
- **Storage**: 100GB free space (models require 20-30GB)
- **Network**: Stable broadband for model downloads

### Expected Resource Usage

| Component | CPU | Memory | Storage |
|-----------|-----|--------|---------|
| Menlo API | 1-2 cores | 512MB | 1GB |
| PostgreSQL | 1 core | 256MB | 5-10GB |
| Ollama + Models | 2-4 cores | 8-12GB | 20-30GB |
| **Total** | **4-7 cores** | **9-13GB** | **26-41GB** |

### Performance Notes

- **First startup**: Model download takes 10-30 minutes
- **Subsequent startups**: ~30 seconds to full readiness
- **AI inference**: 2-10 seconds per request (depending on model and query)

## 🔐 Security Considerations

### Network Security

- Ollama and PostgreSQL are not exposed externally
- Only Menlo API exposed via Cloudflare Tunnel
- SSH access protected by key-based authentication
- Windows Firewall configured for SSH only

### Data Security

- All data remains local (no cloud databases)
- AI processing happens locally (no external AI APIs)
- Database backups stored locally in `$env:USERPROFILE\menlo\backups`

### Access Control

- Cloudflare Access can be configured for additional authentication
- MFA recommended for Cloudflare account
- Regular SSH key rotation recommended

## 📞 Support

For issues specific to this deployment:

1. **Check logs** using the commands above
2. **Verify setup** by running `setup-windows-podman.ps1 -WhatIf`
3. **Test connectivity** using health check URLs
4. **Review GitHub Actions** logs for deployment issues

For general Menlo application issues, see the main [documentation](../docs/README.md).

---

*This deployment implements the cost-conscious, privacy-first architecture defined in [ADR-001](../docs/decisions/adr-001-hosting-strategy.md) specifically for Windows 10 Home environments.*
