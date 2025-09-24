# Menlo Deployment Guide

This directory contains deployment scripts and configuration for the Menlo Home Management application, implementing the hybrid cloud-local architecture.

## 📁 Contents

| File | Purpose | Platform |
|------|---------|----------|
| `setup-home-server.sh` | Linux server setup script (Docker) | Linux/Ubuntu |
| `podman-setup.md` | Enhanced Podman setup guide (recommended) | Linux/Ubuntu |
| `docker-compose.prod.yml` | Production Compose configuration | Linux/WSL2/Podman |
| `deploy-windows.ps1` | Windows/WSL2 deployment script (Podman) | Windows 10 Home |
| `discover-host.ps1` | Find your HOME_SERVER_HOST IP address | Windows |
| `windows-setup.md` | Complete Windows 10 Home setup guide (Podman) | Windows |

## 🖥️ Windows 10 Home Support

For Windows 10 Home users, we provide specialized scripts and documentation:

### Quick Start (Windows)

1. **Discover your server IP:**
   ```powershell
   .\discover-host.ps1
   ```

2. **Test deployment locally:**
   ```powershell
   .\deploy-windows.ps1 -ImageTag "latest" -ImageName "your-username/menlo-api"
   ```

3. **Complete Windows setup:**
   Follow the [Windows Setup Guide](windows-setup.md) for full WSL2 + Docker configuration

### Podman Benefits (Recommended)

- **Enhanced Security**: Rootless containers by default
- **No Daemon**: No privileged background service
- **Systemd Integration**: Native Linux service management
- **Docker Compatible**: Drop-in replacement for Docker commands

### Windows-Specific Features

- **WSL2 Integration**: Seamless Podman operation on Windows 10 Home
- **PowerShell Scripts**: Native Windows deployment automation
- **IP Discovery**: Automatic network configuration detection
- **GitHub Actions**: Windows-compatible CI/CD deployment

## 🏠 Home Server Setup

### Prerequisites

- Ubuntu 20.04+ or Debian 11+ server
- Minimum 8GB RAM, 4-core CPU, 100GB storage
- Stable internet connection
- Sudo privileges

### Initial Setup

Run the automated setup script:

```bash
# Download and run the setup script
wget https://raw.githubusercontent.com/DigiBanks99/menlo/main/deployment/setup-home-server.sh
chmod +x setup-home-server.sh
./setup-home-server.sh
```

This script will:
- Install Docker and Docker Compose
- Create the `menlo` user and application directory
- Install and configure Cloudflare Tunnel
- Set up firewall rules and security
- Create backup and health check scripts
- Configure systemd services

### Manual Configuration

After running the setup script:

1. **Configure Environment Variables**:
   ```bash
   sudo -u menlo cp /opt/menlo/.env.template /opt/menlo/.env
   sudo -u menlo nano /opt/menlo/.env
   ```

2. **Set up Cloudflare Tunnel**:
   ```bash
   # Login to Cloudflare
   cloudflared tunnel login
   
   # Create tunnel
   cloudflared tunnel create menlo-api
   
   # Configure tunnel (example)
   cloudflared tunnel route dns menlo-api api.menlo.yourdomain.com
   
   # Install as service
   sudo cloudflared service install
   ```

3. **Configure SSH Access for GitHub Actions**:
   ```bash
   # Add GitHub Actions public key to authorized_keys
   sudo -u menlo mkdir -p /home/menlo/.ssh
   echo "YOUR_GITHUB_ACTIONS_PUBLIC_KEY" | sudo -u menlo tee -a /home/menlo/.ssh/authorized_keys
   sudo -u menlo chmod 600 /home/menlo/.ssh/authorized_keys
   ```

## 🔐 Security Configuration

### Firewall Rules

The setup script configures UFW with:
- SSH access (port 22)
- API access (port 8080) - internal only
- All other ports blocked
- Cloudflare Tunnel eliminates need for external port exposure

### Fail2Ban

Configured to protect against:
- SSH brute force attacks
- HTTP authentication failures
- Persistent scanning attempts

### SSL/TLS

SSL termination handled by Cloudflare:
- Automatic certificate management
- Edge-to-edge encryption
- Protection against common attacks

## 📦 Application Deployment

### Deployment Process

The GitHub Actions workflow handles deployment automatically:

1. **Build Container**: Creates Docker image from source
2. **Push to Registry**: Uploads to GitHub Container Registry
3. **Deploy to Server**: SSH deployment with zero-downtime strategy
4. **Health Verification**: Automated health checks
5. **Rollback on Failure**: Automatic rollback if deployment fails

### Manual Deployment

For manual deployment or troubleshooting:

```bash
# Switch to menlo user
sudo -u menlo -i

# Navigate to application directory
cd /opt/menlo

# Pull latest images
docker compose -f docker-compose.prod.yml pull

# Deploy with zero downtime
docker compose -f docker-compose.prod.yml up -d

# Check status
docker compose -f docker-compose.prod.yml ps

# View logs
docker compose -f docker-compose.prod.yml logs -f menlo-api
```

## 📊 Monitoring and Maintenance

### Health Checks

Run health checks:
```bash
/opt/menlo/healthcheck.sh
```

### Backup Management

Manual backup:
```bash
/opt/menlo/backup.sh
```

View backups:
```bash
ls -la /opt/menlo/backups/
```

Restore from backup:
```bash
# Restore database
gunzip -c /opt/menlo/backups/menlo_db_TIMESTAMP.sql.gz | \
  docker compose -f /opt/menlo/docker-compose.prod.yml exec -T postgres \
  psql -U $POSTGRES_USER -d $POSTGRES_DB
```

### Log Management

View application logs:
```bash
# Real-time logs
docker compose -f /opt/menlo/docker-compose.prod.yml logs -f

# Specific service logs
docker compose -f /opt/menlo/docker-compose.prod.yml logs -f menlo-api
docker compose -f /opt/menlo/docker-compose.prod.yml logs -f postgres
docker compose -f /opt/menlo/docker-compose.prod.yml logs -f ollama
```

### System Maintenance

Update system packages:
```bash
sudo apt update && sudo apt upgrade -y
```

Update Docker images:
```bash
cd /opt/menlo
sudo -u menlo docker compose -f docker-compose.prod.yml pull
sudo -u menlo docker compose -f docker-compose.prod.yml up -d
sudo -u menlo docker system prune -f
```

## 🌐 Cloudflare Tunnel Configuration

### Tunnel Setup

Create tunnel configuration file:
```yaml
# ~/.cloudflared/config.yml
tunnel: menlo-api
credentials-file: ~/.cloudflared/menlo-api.json

ingress:
  - hostname: api.menlo.yourdomain.com
    service: http://localhost:8080
  - service: http_status:404
```

### DNS Configuration

Configure DNS records in Cloudflare dashboard:
- `api.menlo.yourdomain.com` → CNAME to tunnel
- Enable proxy mode for security and caching

### Tunnel Monitoring

Check tunnel status:
```bash
sudo systemctl status cloudflared
cloudflared tunnel info menlo-api
```

## 🚨 Troubleshooting

### Common Issues

**Docker Permission Issues**:
```bash
sudo usermod -aG docker menlo
# Logout and login again
```

**Database Connection Issues**:
```bash
# Check PostgreSQL container
docker compose -f /opt/menlo/docker-compose.prod.yml exec postgres pg_isready

# Check connection string in .env file
grep DATABASE_CONNECTION_STRING /opt/menlo/.env
```

**Ollama Model Issues**:
```bash
# Pull required models
docker compose -f /opt/menlo/docker-compose.prod.yml exec ollama ollama pull microsoft/phi-4:latest
```

**Cloudflare Tunnel Issues**:
```bash
# Check tunnel logs
sudo journalctl -u cloudflared -f

# Restart tunnel
sudo systemctl restart cloudflared
```

### Log Locations

- Application logs: `/opt/menlo/logs/`
- Docker logs: `docker compose logs`
- System logs: `/var/log/syslog`
- Tunnel logs: `journalctl -u cloudflared`

### Emergency Procedures

**Complete System Restart**:
```bash
sudo systemctl stop menlo
sudo systemctl stop cloudflared
sudo reboot
```

**Emergency Rollback**:
```bash
cd /opt/menlo
# Find latest backup
ls -la backups/docker-compose.*.yml | tail -1
# Restore configuration
sudo -u menlo cp backups/docker-compose.TIMESTAMP.yml docker-compose.prod.yml
sudo systemctl restart menlo
```

## 📞 Support

For deployment issues:
1. Check the troubleshooting section above
2. Review logs using the commands provided
3. Consult the [Architecture Document](../docs/explanations/architecture-document.md)
4. Create an issue in the GitHub repository

---

*This deployment configuration implements the cost-conscious, privacy-first architecture defined in [ADR-001](../docs/decisions/adr-001-hosting-strategy.md).*
